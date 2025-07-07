using AssetsTools.NET;

using BA_MU.Bundle;
using BA_MU.ImportExport.Assets;

using BA_MU.Core.Models;
using BA_MU.Core.Utils;
using BA_MU.Helpers;

namespace BA_MU.Core.Services;

public class AssetImport
{
    public async Task<int> ImportAssets(Load loader, List<Match> matches)
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
        var refManager = new RefTypeManager();
        refManager.FromTypeTree(assetsFileInstance.file.Metadata);
        
        var importedCount = 0;

        Logs.Info("Importing JSON assets...");

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

                var jsonFilePath = Path.Combine(dumpsDir, match.JsonFileName);
                if (!File.Exists(jsonFilePath))
                {
                    Logs.Error($"JSON file not found: {match.JsonFileName}");
                    continue;
                }

                var templateField = assetsManager.GetTemplateBaseField(assetsFileInstance, targetAssetInfo);
                if (templateField == null)
                {
                    Logs.Error($"Failed to get template field for asset {match.Name}");
                    continue;
                }

                Logs.Debug($"Processing: {match.Name} ({match.Type})");

                await using var jsonStream = File.OpenRead(jsonFilePath);
                var importer = new Import(jsonStream, refManager);
                var importedData = importer.ImportJsonAsset(templateField, out var exceptionMessage);

                if (importedData == null)
                {
                    Logs.Error($"Failed to import JSON for {match.Name}: {exceptionMessage}");
                    continue;
                }

                targetAssetInfo.SetNewData(importedData);
                importedCount++;
                Logs.Debug($"Imported: {match.Name} ({match.Type})");
            }
            catch (Exception ex)
            {
                Logs.Error($"Error importing asset {match.PatchId}", ex);
            }
        }

        return importedCount;
    }
}