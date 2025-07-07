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
            
            var textureTemp = assetsManager.GetTemplateBaseField(assetsFileInstance, assetInfo);
            if (textureTemp == null)
            {
                Logs.Error($"Failed to get template field for {assetInfo.PathId}");
                return false;
            }
            
            var imageData = textureTemp.Children.FirstOrDefault(f => f.Name == "image data");
            if (imageData == null)
            {
                Logs.Error($"No image data found for {assetInfo.PathId}");
                return false;
            }

            imageData.ValueType = AssetValueType.ByteArray;
            Logs.Debug("Image data field set to ByteArray");

            var platformBlob = textureTemp.Children.FirstOrDefault(f => f.Name == "m_PlatformBlob");
            if (platformBlob != null)
            {
                var platformBlobArray = platformBlob.Children[0];
                platformBlobArray.ValueType = AssetValueType.ByteArray;
                Logs.Debug("Platform blob found and set");
            }

            var texBaseField = assetsManager.GetBaseField(assetsFileInstance, assetInfo);
            if (texBaseField == null)
            {
                Logs.Error($"Failed to get base field for {assetInfo.PathId}");
                return false;
            }

            var texFile = TextureFile.ReadTextureFile(texBaseField);
            Logs.Debug($"Texture format: {texFile.m_TextureFormat}");
            Logs.Debug($"Texture dimensions: {texFile.m_Width}x{texFile.m_Height}");

            if (texFile is { m_Width: 0, m_Height: 0 })
            {
                Logs.Error("Invalid texture dimensions");
                return false;
            }

            using var outputStream = File.OpenWrite(filePath);
            Logs.Debug($"Created output stream to {filePath}");

            var encTextureData = texFile.FillPictureData(assetsFileInstance);
            if (encTextureData == null || encTextureData.Length == 0)
            {
                Logs.Error("No texture data obtained");
                return false;
            }
            Logs.Debug($"Got texture data of size: {encTextureData.Length}");

            var success = texFile.DecodeTextureImage(encTextureData, outputStream, exportType, 100);
            Logs.Debug($"Decode result: {success}");
            
            return success;
        }
        catch (Exception ex)
        {
            Logs.Debug($"Exception during export: {ex.Message}");
            Logs.Debug($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }
}