using AssetsTools.NET;
using Newtonsoft.Json.Linq;

namespace BA_MU.ImportExport.Assets;

public class Import
{
    private readonly RefTypeManager? _refMan;
    private readonly Stream _stream;
    private readonly StreamReader _streamReader;

    public Import(Stream readStream, RefTypeManager? refMan = null)
    {
        _stream = readStream;
        _streamReader = new StreamReader(_stream);
        _refMan = refMan;
    }

    public byte[] ImportRawAsset()
    {
        using var ms = new MemoryStream();
        _stream.CopyTo(ms);
        return ms.ToArray();
    }

    public byte[]? ImportJsonAsset(AssetTypeTemplateField tempField, out string? exceptionMessage)
    {
        using var ms = new MemoryStream();
        var writer = new AssetsFileWriter(ms)
        {
            BigEndian = false
        };

        try
        {
            var jsonText = _streamReader.ReadToEnd();
            var token = JToken.Parse(jsonText);

            RecurseJsonImport(writer, tempField, token);
            exceptionMessage = null;
        }
        catch (Exception ex)
        {
            exceptionMessage = ex.ToString();
            return null;
        }

        return ms.ToArray();
    }

    private void RecurseJsonImport(AssetsFileWriter writer, AssetTypeTemplateField tempField, JToken token)
    {
        var align = tempField.IsAligned;

        if (tempField.Children.Count == 1 && tempField.Children[0].IsArray &&
            token.Type == JTokenType.Array)
        {
            RecurseJsonImport(writer, tempField.Children[0], token);
            return;
        }

        switch (tempField)
        {
            case { HasValue: false, IsArray: false }:
            {
                foreach (var childTempField in tempField.Children)
                {
                    var childToken = token[childTempField.Name];

                    if (childToken == null)
                    {
                        WriteDefaultValue(writer, childTempField);
                        Console.WriteLine($"Warning: Missing field {childTempField.Name} in JSON, using default value");
                    }
                    else
                    {
                        RecurseJsonImport(writer, childTempField, childToken);
                    }
                }

                if (align) writer.Align();
                break;
            }
            case { HasValue: true, ValueType: AssetValueType.ManagedReferencesRegistry }:
                JsonImportManagedReferencesRegistry(writer, tempField, token);
                break;
            default:
            {
                switch (tempField.ValueType)
                {
                    case AssetValueType.Bool:
                        writer.Write((bool)token);
                        break;
                    case AssetValueType.UInt8:
                        writer.Write((byte)token);
                        break;
                    case AssetValueType.Int8:
                        writer.Write((sbyte)token);
                        break;
                    case AssetValueType.UInt16:
                        writer.Write((ushort)token);
                        break;
                    case AssetValueType.Int16:
                        writer.Write((short)token);
                        break;
                    case AssetValueType.UInt32:
                        writer.Write((uint)token);
                        break;
                    case AssetValueType.Int32:
                        writer.Write((int)token);
                        break;
                    case AssetValueType.UInt64:
                        writer.Write((ulong)token);
                        break;
                    case AssetValueType.Int64:
                        writer.Write((long)token);
                        break;
                    case AssetValueType.Float:
                        writer.Write((float)token);
                        break;
                    case AssetValueType.Double:
                        writer.Write((double)token);
                        break;
                    case AssetValueType.String:
                        align = true;
                        writer.WriteCountStringInt32((string?)token ?? "");
                        break;
                    case AssetValueType.ByteArray:
                        var byteArrayJArray = (JArray?)token ?? [];
                        var byteArrayData = new byte[byteArrayJArray.Count];
                        for (var i = 0; i < byteArrayJArray.Count; i++) 
                            byteArrayData[i] = (byte)byteArrayJArray[i];
                        writer.Write(byteArrayData.Length);
                        writer.Write(byteArrayData);
                        break;
                }

                if (tempField.IsArray && tempField.ValueType != AssetValueType.ByteArray)
                {
                    var childTempField = tempField.Children[1];
                    var tokenArray = (JArray?)token;

                    if (tokenArray == null)
                        throw new Exception($"Field {tempField.Name} was not an array in json.");

                    writer.Write(tokenArray.Count);
                    foreach (var childToken in tokenArray.Children())
                        RecurseJsonImport(writer, childTempField, childToken);
                }

                if (align) writer.Align();
                break;
            }
        }
    }
    
    private static void WriteDefaultValue(AssetsFileWriter writer, AssetTypeTemplateField tempField)
    {
        var align = tempField.IsAligned;

        switch (tempField)
        {
            case { HasValue: false, IsArray: false }:
            {
                foreach (var childTempField in tempField.Children)
                {
                    WriteDefaultValue(writer, childTempField);
                }
                if (align) writer.Align();
                break;
            }
            case { HasValue: true, ValueType: AssetValueType.ManagedReferencesRegistry }:
                writer.Write(1);
                break;
            default:
            {
                switch (tempField.ValueType)
                {
                    case AssetValueType.Bool:
                        writer.Write(false);
                        break;
                    case AssetValueType.UInt8:
                        writer.Write((byte)0);
                        break;
                    case AssetValueType.Int8:
                        writer.Write((sbyte)0);
                        break;
                    case AssetValueType.UInt16:
                        writer.Write((ushort)0);
                        break;
                    case AssetValueType.Int16:
                        writer.Write((short)0);
                        break;
                    case AssetValueType.UInt32:
                        writer.Write((uint)0);
                        break;
                    case AssetValueType.Int32:
                        writer.Write(0);
                        break;
                    case AssetValueType.UInt64:
                        writer.Write((ulong)0);
                        break;
                    case AssetValueType.Int64:
                        writer.Write((long)0);
                        break;
                    case AssetValueType.Float:
                        writer.Write(0.0f);
                        break;
                    case AssetValueType.Double:
                        writer.Write(0.0);
                        break;
                    case AssetValueType.String:
                        align = true;
                        writer.WriteCountStringInt32("");
                        break;
                    case AssetValueType.ByteArray:
                        writer.Write(0);
                        break;
                }

                if (tempField.IsArray && tempField.ValueType != AssetValueType.ByteArray)
                {
                    writer.Write(0);
                }

                if (align) writer.Align();
                break;
            }
        }
    }

    private void JsonImportManagedReferencesRegistry(AssetsFileWriter writer, AssetTypeTemplateField tempField,
        JToken token)
    {
        var version = (int)ExpectAndReadField(token, "version", tempField);
        if (version is < 1 or > 2) throw new Exception($"ManagedReferencesRegistry version {version} is invalid.");

        var refIdsArray = (JArray)ExpectAndReadField(token, "RefIds", tempField);

        writer.Write(version);
        var childCount = refIdsArray.Count;

        if (version != 1) writer.Write(childCount);

        for (var i = 0; i < childCount; i++)
        {
            var refdObjectToken = refIdsArray[i];
            var rid = (long)ExpectAndReadField(refdObjectToken, "rid", tempField);
            if (version == 1)
            {
                if (rid != i) throw new Exception($"Field rid must be consecutive. Expected {i}, found {rid}.");
            }
            else
            {
                writer.Write(rid);
            }

            var typeToken = ExpectAndReadField(refdObjectToken, "type", tempField);
            var typeRef = new AssetTypeReference
            {
                ClassName = (string?)ExpectAndReadField(typeToken, "class", tempField) ?? string.Empty,
                Namespace = (string?)ExpectAndReadField(typeToken, "ns", tempField) ?? string.Empty,
                AsmName = (string?)ExpectAndReadField(typeToken, "asm", tempField) ?? string.Empty
            };

            var dataToken = ExpectAndReadField(refdObjectToken, "data", tempField);

            typeRef.WriteAsset(writer);
            if (typeRef is { ClassName: "", Namespace: "", AsmName: "" }) continue;

            if (_refMan != null)
            {
                var objectTempField = _refMan.GetTemplateField(typeRef);
                if (objectTempField == null)
                    throw new Exception(
                        $"Failed to get managed reference type. Wanted {typeRef.ClassName}.{typeRef.Namespace}" +
                        $" in {typeRef.AsmName} but got a null result.");

                RecurseJsonImport(writer, objectTempField, dataToken);
            }
            else
            {
                Console.WriteLine(
                    $"Warning: No RefTypeManager available. Skipping managed reference data for {typeRef.ClassName}");
            }
        }

        if (version == 1)
            AssetTypeReference.TERMINUS.WriteAsset(writer);
        else
            writer.Align();
    }

    private static JToken ExpectAndReadField(JToken token, string name, AssetTypeTemplateField? tempField)
    {
        var versionToken = token[name];
        if (versionToken != null) return versionToken;

        if (tempField == null) throw new Exception($"Missing field {name} in JSON.");

        throw new Exception($"Missing field {name} in JSON. Parent field is {tempField.Type} {tempField.Name}.");
    }
}