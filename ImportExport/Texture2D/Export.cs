using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BA_MU.Helpers;

namespace BA_MU.ImportExport.Texture2D;

public static class Export
{
    public static bool ExportTexture(AssetsFileInstance assetsFileInstance, AssetsManager assetsManager, AssetFileInfo assetInfo, string filePath, ImageExportType exportType)
    {
        try
        {
            Logs.Debug($"Starting export for asset {assetInfo.PathId}");
            
            var textureTemplate = GetTextureTemplate(assetsManager, assetsFileInstance, assetInfo);
            if (textureTemplate == null)
                return false;

            if (!ConfigureTemplateFields(textureTemplate, assetInfo.PathId))
                return false;

            var textureBaseField = GetTextureBaseField(assetsManager, assetsFileInstance, assetInfo);
            if (textureBaseField == null)
                return false;

            var textureFile = CreateTextureFile(textureBaseField);
            if (textureFile == null)
                return false;

            if (!ValidateTextureDimensions(textureFile))
                return false;

            return ExportTextureData(textureFile, assetsFileInstance, filePath, exportType);
        }
        catch (Exception ex)
        {
            Logs.Debug($"Exception during export: {ex.Message}");
            Logs.Debug($"Stack trace: {ex.StackTrace}");
            return false;
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
            Logs.Error($"No image data found for {assetId}");
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

        Logs.Debug($"Texture format: {textureFile.m_TextureFormat}");
        Logs.Debug($"Texture dimensions: {textureFile.m_Width}x{textureFile.m_Height}");
        return textureFile;
    }

    private static bool ValidateTextureDimensions(TextureFile textureFile)
    {
        if (textureFile is not { m_Width: 0, m_Height: 0 }) return true;
        Logs.Error("Invalid texture dimensions");
        return false;

    }

    private static bool ExportTextureData(TextureFile textureFile, AssetsFileInstance assetsFileInstance, string filePath, ImageExportType exportType)
    {
        using var outputStream = File.OpenWrite(filePath);
        Logs.Debug($"Created output stream to {filePath}");

        var textureData = GetTextureData(textureFile, assetsFileInstance);
        if (textureData == null)
            return false;

        var success = textureFile.DecodeTextureImage(textureData, outputStream, exportType);
        Logs.Debug($"Decode result: {success}");
        
        return success;
    }

    private static byte[]? GetTextureData(TextureFile textureFile, AssetsFileInstance assetsFileInstance)
    {
        var textureData = textureFile.FillPictureData(assetsFileInstance);
        if (textureData == null || textureData.Length == 0)
        {
            Logs.Error("No texture data obtained");
            return null;
        }

        Logs.Debug($"Got texture data of size: {textureData.Length}");
        return textureData;
    }
}