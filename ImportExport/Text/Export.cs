using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BA_MU.Helpers;


namespace BA_MU.ImportExport.Text;

public static class Export
{
    public static bool ExportTextAsset(AssetsFileInstance assetsFileInstance, AssetsManager assetsManager, AssetFileInfo assetInfo, string filePath)
    {
        try
        {
            Logs.Debug($"Starting TextAsset export for asset {assetInfo.PathId}");
            
            var textAssetBaseField = GetTextAssetBaseField(assetsManager, assetsFileInstance, assetInfo);
            if (textAssetBaseField == null)
                return false;

            var textData = ExtractTextData(textAssetBaseField, assetInfo.PathId);
            return textData != null && WriteTextToFile(textData, filePath);
        }
        catch (Exception ex)
        {
            Logs.Error($"Exception during TextAsset export: {ex.Message}");
            Logs.Debug($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    private static AssetTypeValueField? GetTextAssetBaseField(AssetsManager assetsManager, AssetsFileInstance assetsFileInstance, AssetFileInfo assetInfo)
    {
        var baseField = assetsManager.GetBaseField(assetsFileInstance, assetInfo);
        if (baseField != null)
            return baseField;

        Logs.Error($"Failed to get base field for TextAsset {assetInfo.PathId}");
        return null;
    }

    private static byte[]? ExtractTextData(AssetTypeValueField textAssetBaseField, long assetId)
    {
        try
        {
            var scriptField = textAssetBaseField["m_Script"];
            if (scriptField == null)
            {
                Logs.Error($"No m_Script field found for asset {assetId}");
                return null;
            }

            var textData = scriptField.AsByteArray;
            if (textData == null || textData.Length == 0)
            {
                Logs.Warn($"Empty text data for asset {assetId}");
                return [];
            }

            Logs.Debug($"Extracted text data of size: {textData.Length} bytes");
            return textData;
        }
        catch (Exception ex)
        {
            Logs.Error($"Failed to extract text data for asset {assetId}: {ex.Message}");
            return null;
        }
    }


    private static bool WriteTextToFile(byte[] textData, string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(filePath, textData);
            Logs.Debug($"Successfully wrote {textData.Length} bytes to {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logs.Error($"Failed to write file {filePath}: {ex.Message}");
            return false;
        }
    }
}