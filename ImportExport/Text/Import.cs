using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BA_MU.Helpers;


namespace BA_MU.ImportExport.Text;

public static class Import
{
    public static bool ImportTextAsset(AssetsFileInstance assetsFileInstance, AssetsManager assetsManager, AssetFileInfo assetInfo, string filePath)
    {
        try
        {
            Logs.Debug($"Starting TextAsset import for asset {assetInfo.PathId}");

            if (!File.Exists(filePath))
            {
                Logs.Error($"Import file not found: {filePath}");
                return false;
            }

            var baseField = assetsManager.GetBaseField(assetsFileInstance, assetInfo);
            if (baseField == null)
            {
                Logs.Error($"Failed to get base field for TextAsset {assetInfo.PathId}");
                return false;
            }

            var newBytes = File.ReadAllBytes(filePath);
            baseField["m_Script"].AsByteArray = newBytes;
            
            var replacer = new ContentReplacerFromBuffer(baseField.WriteToByteArray());
            assetInfo.Replacer = replacer;

            Logs.Debug($"Successfully created replacer for TextAsset {assetInfo.PathId} from {filePath}");
            return true;
        }
        catch (Exception ex)
        { 
            Logs.Error($"Exception during TextAsset import: {ex.Message}");
            Logs.Debug($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }
}