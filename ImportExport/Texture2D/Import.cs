using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BA_MU.Helpers;

namespace BA_MU.ImportExport.Texture2D;

public static class Import
{
    public static Task<bool> ImportSingle(AssetsFileInstance assetsFileInstance, AssetsManager assetsManager,
        AssetFileInfo assetInfo, string filePath)
    {
        try
        {
            Logs.Debug($"Starting import for asset {assetInfo.PathId}");

            var textureTemp = assetsManager.GetTemplateBaseField(assetsFileInstance, assetInfo);
            if (textureTemp == null)
            {
                Logs.Debug($"Failed to get template field for {assetInfo.PathId}");
                return Task.FromResult(false);
            }

            var imageData = textureTemp.Children.FirstOrDefault(f => f.Name == "image data");
            if (imageData == null)
            {
                Logs.Debug($"No image data found for {assetInfo.PathId}");
                return Task.FromResult(false);
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
                Logs.Debug($"Failed to get base field for {assetInfo.PathId}");
                return Task.FromResult(false);
            }

            var texFile = TextureFile.ReadTextureFile(texBaseField);
            Logs.Debug($"Original texture format: {texFile.m_TextureFormat}");
            Logs.Debug($"Original dimensions: {texFile.m_Width}x{texFile.m_Height}");

            if (!File.Exists(filePath))
            {
                Logs.Debug($"Import file not found: {filePath}");
                return Task.FromResult(false);
            }

            try
            {
                Logs.Debug($"Encoding texture from file: {filePath}");
                texFile.EncodeTextureImage(filePath);
                Logs.Debug("Successfully encoded texture");

                Logs.Debug("Writing texture data back to asset");
                texFile.WriteTo(texBaseField);
                Logs.Debug("Successfully wrote texture data");

                var modifiedData = texBaseField.WriteToByteArray();
                var replacer = new ContentReplacerFromBuffer(modifiedData);

                assetInfo.Replacer = replacer;
                Logs.Debug("Asset replacer set successfully");

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logs.Debug($"Failed to encode/write texture: {ex.Message}");
                return Task.FromResult(false);
            }
        }
        catch (Exception ex)
        {
            Logs.Debug($"Exception during import: {ex.Message}");
            Logs.Debug($"Stack trace: {ex.StackTrace}");
            return Task.FromResult(false);
        }
    }
}