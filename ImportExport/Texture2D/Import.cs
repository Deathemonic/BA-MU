using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BA_MU.Helpers;

namespace BA_MU.ImportExport.Texture2D;

public static class Import
{
    public static Task<bool> ImportTexture(AssetsFileInstance assetsFileInstance, AssetsManager assetsManager, AssetFileInfo assetInfo, string filePath)
    {
        try
        {
            Logs.Debug($"Starting import for asset {assetInfo.PathId}");

            var textureTemplate = GetTextureTemplate(assetsManager, assetsFileInstance, assetInfo);
            if (textureTemplate == null || !ConfigureTemplateFields(textureTemplate, assetInfo.PathId))
                return Task.FromResult(false);

            var textureBaseField = GetTextureBaseField(assetsManager, assetsFileInstance, assetInfo);
            if (textureBaseField == null)
                return Task.FromResult(false);

            var textureFile = CreateTextureFile(textureBaseField);
            if (textureFile == null || !ValidateImportFile(filePath))
                return Task.FromResult(false);

            return Task.FromResult(ProcessTextureImport(textureFile, textureBaseField, assetInfo, filePath));
        }
        catch (Exception ex)
        {
            Logs.Error($"Exception during import: {ex.Message}");
            Logs.Error($"Stack trace: {ex.StackTrace}");
            return Task.FromResult(false);
        }
    }

    private static AssetTypeTemplateField? GetTextureTemplate(AssetsManager assetsManager, AssetsFileInstance assetsFileInstance, AssetFileInfo assetInfo)
    {
        var textureTemplate = assetsManager.GetTemplateBaseField(assetsFileInstance, assetInfo);
        if (textureTemplate != null)
            return textureTemplate;

        Logs.Error($"Failed to get template field for {assetInfo.PathId}");
        return null;
    }

    private static bool ConfigureTemplateFields(AssetTypeTemplateField textureTemplate, long assetId)
    {
        if (!ConfigureImageDataField(textureTemplate, assetId))
            return false;

        ConfigurePlatformBlobField(textureTemplate);
        return true;
    }

    private static bool ConfigureImageDataField(AssetTypeTemplateField textureTemplate, long assetId)
    {
        var imageData = textureTemplate.Children.FirstOrDefault(f => f.Name == "image data");
        if (imageData == null)
        {
            Logs.Warn($"No image data found for {assetId}");
            return false;
        }

        imageData.ValueType = AssetValueType.ByteArray;
        Logs.Debug("Image data field set to ByteArray");
        return true;
    }

    private static void ConfigurePlatformBlobField(AssetTypeTemplateField textureTemplate)
    {
        var platformBlob = textureTemplate.Children.FirstOrDefault(f => f.Name == "m_PlatformBlob");
        if (platformBlob == null)
            return;

        var platformBlobArray = platformBlob.Children[0];
        platformBlobArray.ValueType = AssetValueType.ByteArray;
        Logs.Debug("Platform blob found and set");
    }

    private static AssetTypeValueField? GetTextureBaseField(AssetsManager assetsManager, AssetsFileInstance assetsFileInstance, AssetFileInfo assetInfo)
    {
        var baseField = assetsManager.GetBaseField(assetsFileInstance, assetInfo);
        if (baseField != null)
            return baseField;

        Logs.Error($"Failed to get base field for {assetInfo.PathId}");
        return null;
    }

    private static TextureFile? CreateTextureFile(AssetTypeValueField textureBaseField)
    {
        var textureFile = TextureFile.ReadTextureFile(textureBaseField);
        if (textureFile == null)
            return null;

        Logs.Debug($"Original texture format: {textureFile.m_TextureFormat}");
        Logs.Debug($"Original dimensions: {textureFile.m_Width}x{textureFile.m_Height}");
        return textureFile;
    }

    private static bool ValidateImportFile(string filePath)
    {
        if (File.Exists(filePath))
            return true;

        Logs.Debug($"Import file not found: {filePath}");
        return false;
    }

    private static bool ProcessTextureImport(TextureFile textureFile, AssetTypeValueField textureBaseField, AssetFileInfo assetInfo, string filePath)
    {
        if (!EncodeTextureFromFile(textureFile, filePath))
            return false;

        if (!WriteTextureToAsset(textureFile, textureBaseField))
            return false;

        return ApplyTextureChanges(textureBaseField, assetInfo);
    }

    private static bool EncodeTextureFromFile(TextureFile textureFile, string filePath)
    {
        try
        {
            Logs.Debug($"Encoding texture from file: {filePath}");
            textureFile.EncodeTextureImage(filePath);
            Logs.Debug("Successfully encoded texture");
            return true;
        }
        catch (Exception ex)
        {
            Logs.Error($"Failed to encode texture: {ex.Message}");
            return false;
        }
    }

    private static bool WriteTextureToAsset(TextureFile textureFile, AssetTypeValueField textureBaseField)
    {
        try
        {
            Logs.Debug("Writing texture data back to asset");
            textureFile.WriteTo(textureBaseField);
            Logs.Debug("Successfully wrote texture data");
            return true;
        }
        catch (Exception ex)
        {
            Logs.Error($"Failed to write texture data: {ex.Message}");
            return false;
        }
    }

    private static bool ApplyTextureChanges(AssetTypeValueField textureBaseField, AssetFileInfo assetInfo)
    {
        try
        {
            var modifiedData = textureBaseField.WriteToByteArray();
            var replacer = new ContentReplacerFromBuffer(modifiedData);

            assetInfo.Replacer = replacer;
            Logs.Debug("Asset replacer set successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logs.Error($"Failed to apply texture changes: {ex.Message}");
            return false;
        }
    }
}