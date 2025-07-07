using AssetsTools.NET;
using Newtonsoft.Json.Linq;

namespace BA_MU.ImportExport.Assets;

public class Export
{
    private readonly Stream _stream;
    private readonly StreamWriter _streamWriter;

    public Export(Stream writeStream)
    {
        _stream = writeStream;
        _streamWriter = new StreamWriter(_stream);
    }

    public void DumpRawAsset(AssetsFileReader reader, long position, uint size)
    {
        var assetFs = reader.BaseStream;
        assetFs.Position = position;

        var buf = new byte[4096];
        var bytesLeft = (int)size;
        while (bytesLeft > 0)
        {
            var readSize = assetFs.Read(buf, 0, Math.Min(bytesLeft, buf.Length));
            _stream.Write(buf, 0, readSize);
            bytesLeft -= readSize;
        }
    }

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
                return JsonDumpManagedReferencesRegistry(field, uabeFlavor);
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

    private JObject JsonDumpManagedReferencesRegistry(AssetTypeValueField field, bool uabeFlavor = false)
    {
        var registry = field.Value.AsManagedReferencesRegistry;

        if (registry.version is >= 1 and <= 2)
        {
            var jArrayRefs = new JArray();
            foreach (var refObj in registry.references)
            {
                var typeRef = refObj.type;

                var jObjManagedType = new JObject
                {
                    { "class", typeRef.ClassName },
                    { "ns", typeRef.Namespace },
                    { "asm", typeRef.AsmName }
                };

                var jObjData = new JObject();
                foreach (var child in refObj.data)
                {
                    jObjData.Add(child.FieldName, RecurseJsonDump(child, uabeFlavor));
                }

                JObject jObjRefObject;
                if (registry.version == 1)
                {
                    jObjRefObject = new JObject
                    {
                        { "type", jObjManagedType },
                        { "data", jObjData }
                    };
                }
                else
                {
                    jObjRefObject = new JObject
                    {
                        { "rid", refObj.rid },
                        { "type", jObjManagedType },
                        { "data", jObjData }
                    };
                }

                jArrayRefs.Add(jObjRefObject);
            }

            var jObjReferences = new JObject
            {
                { "version", registry.version },
                { "RefIds", jArrayRefs }
            };

            return jObjReferences;
        }

        throw new NotSupportedException($"Registry version {registry.version} not supported.");
    }
}