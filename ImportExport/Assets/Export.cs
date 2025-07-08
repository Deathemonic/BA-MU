using AssetsTools.NET;
using Newtonsoft.Json.Linq;


namespace BA_MU.ImportExport.Assets;

public class Export(Stream writeStream)
{
    private readonly StreamWriter _streamWriter = new(writeStream);

    public void DumpJsonAsset(AssetTypeValueField baseField)
    {
        var jBaseField = RecurseJsonDump(baseField, false);
        _streamWriter.Write(jBaseField.ToString());
        _streamWriter.Flush();
    }

    private JToken RecurseJsonDump(AssetTypeValueField field, bool uabeFlavor)
    {
        var template = field.TemplateField;
        var isArray = template.IsArray;

        if (isArray)
        {
            var jArray = new JArray();
            if (template.ValueType != AssetValueType.ByteArray)
            {
                foreach (var t in field.Children)
                {
                    jArray.Add(RecurseJsonDump(t, uabeFlavor));
                }
            }
            else
            {
                var byteArrayData = field.AsByteArray;
                foreach (var t in byteArrayData)
                {
                    jArray.Add(t);
                }
            }

            return jArray;
        }

        if (field.Value != null)
        {
            var valueType = field.Value.ValueType;

            if (field.Value.ValueType == AssetValueType.ManagedReferencesRegistry)
                return JsonDumpRefRegistry(field, uabeFlavor);
            object value = valueType switch
            {
                AssetValueType.Bool => field.AsBool,
                AssetValueType.Int8 or
                    AssetValueType.Int16 or
                    AssetValueType.Int32 => field.AsInt,
                AssetValueType.Int64 => field.AsLong,
                AssetValueType.UInt8 or
                    AssetValueType.UInt16 or
                    AssetValueType.UInt32 => field.AsUInt,
                AssetValueType.UInt64 => field.AsULong,
                AssetValueType.String => field.AsString,
                AssetValueType.Float => field.AsFloat,
                AssetValueType.Double => field.AsDouble,
                _ => "invalid value"
            };

            return (JValue)JToken.FromObject(value);

        }

        var jObject = new JObject();
        foreach (var child in field)
        {
            jObject.Add(child.FieldName, RecurseJsonDump(child, uabeFlavor));
        }

        return jObject;
    }

    private JObject JsonDumpRefRegistry(AssetTypeValueField field, bool uabeFlavor = false)
    {
        var registry = field.Value.AsManagedReferencesRegistry;

        if (registry.version is < 1 or > 2)
        {
            throw new NotSupportedException($"Registry version {registry.version} not supported.");
        }
    
        var jArrayRefs = new JArray(
            registry.references.Select(refObj =>
            {
                var jObjManagedType = new JObject
                {
                    { "class", refObj.type.ClassName },
                    { "ns", refObj.type.Namespace },
                    { "asm", refObj.type.AsmName }
                };

                var jObjData = new JObject(
                    refObj.data.Select(child => new JProperty(child.FieldName, RecurseJsonDump(child, uabeFlavor)))
                );

                var jObjRefObject = new JObject
                {
                    { "type", jObjManagedType },
                    { "data", jObjData }
                };
            
                if (registry.version != 1)
                {
                    jObjRefObject.AddFirst(new JProperty("rid", refObj.rid));
                }
            
                return jObjRefObject;
            })
        );

        return new JObject
        {
            { "version", registry.version },
            { "RefIds", jArrayRefs }
        };
    }
}