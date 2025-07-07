using BA_MU.Bundle;
using BA_MU.Core.Models;
using BA_MU.ImportExport.Texture2D;
using BA_MU.Core.Utils;
using BA_MU.Helpers;

namespace BA_MU.Core.Services;

public class TextureImport
{
    public async Task<int> ImportTextures(Load loader, List<Match> matches)
    {
        var dumpsDir = FileName.GetDumpsDirectory();
        if (!Directory.Exists(dumpsDir))
        {
            Logs.Error("Dumps directory not found. Please run parse command first");
            return 0;
        }

        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logs.Error("Failed to get assets file instance for import");
            return 0;
        }

        var assetsManager = loader.GetAssetsManager();
        var importedCount = 0;

        Logs.Info("Importing texture assets...");

        foreach (var match in matches)
        {
            try
            {
                var targetAssetInfo = assetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.PatchId);
                if (targetAssetInfo == null)
                {
                    Logs.Error($"Asset with PathID {match.PatchId} not found in target bundle");
                    continue;
                }

                var cleanAssetName = FileName.Clean(match.Name);
                var pngFilePath = Path.Combine(dumpsDir, $"{cleanAssetName}.png");
                var tgaFilePath = Path.Combine(dumpsDir, $"{cleanAssetName}.tga");
                
                string filePath;
                if (File.Exists(pngFilePath))
                {
                    filePath = pngFilePath;
                }
                else if (File.Exists(tgaFilePath))
                {
                    filePath = tgaFilePath;
                }
                else
                {
                    Logs.Error($"Texture file not found for: {cleanAssetName}");
                    continue;
                }

                Logs.Debug($"Processing texture: {match.Name}");

                var success = await Import.ImportSingle(assetsFileInstance, assetsManager, targetAssetInfo, filePath);
                
                if (!success)
                {
                    Logs.Error($"Failed to import texture for {match.Name}");
                    continue;
                }

                importedCount++;
                Logs.Debug($"Imported texture: {match.Name}");
            }
            catch (Exception ex)
            {
                Logs.Error($"Error importing texture {match.PatchId}", ex);
            }
        }

        return importedCount;
    }
}