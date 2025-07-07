using BA_MU.Bundle;
using BA_MU.ImportExport.Assets;
using BA_MU.Core.Models;
using BA_MU.Core.Utils;
using BA_MU.Helpers;

namespace BA_MU.Core.Services;

public class AssetExport
{
    public async Task<int> ExportAssets(string moddedPath, List<Match> matches)
    {
        FileName.EnsureDumpsDirectoryExists();

        var loader = new Load();
        try
        {
            if (!loader.LoadBundle(moddedPath))
            {
                Logs.Error("Failed to load modded bundle for export");
                return 0;
            }

            var assetsFileInstance = loader.GetAssetsFileInstance();
            if (assetsFileInstance == null)
            {
                Logs.Error("Failed to get assets file instance for export");
                return 0;
            }

            var assetsManager = loader.GetAssetsManager();
            var exportedCount = 0;

            Logs.Info("Exporting JSON dumps...");

            foreach (var match in matches)
            {
                try
                {
                    var assetInfo = assetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.ModdedId);
                    if (assetInfo == null)
                    {
                        Logs.Error($"Asset with PathId {match.ModdedId} not found in modded bundle");
                        continue;
                    }

                    var baseField = assetsManager.GetBaseField(assetsFileInstance, assetInfo);
                    if (baseField == null)
                    {
                        Logs.Error($"Failed to get base field for asset {match.ModdedId}");
                        continue;
                    }

                    var filePath = FileName.GetUniqueFilePath(FileName.GetDumpsDirectory(), match.JsonFileName);

                    await using var fileStream = File.Create(filePath);
                    var exporter = new Export(fileStream);
                    exporter.DumpJsonAsset(baseField);

                    exportedCount++;
                    Logs.Debug($"Exported: {match.Name} ({match.Type})");
                }
                catch (Exception ex)
                {
                    Logs.Error($"Error exporting asset {match.ModdedId}", ex);
                }
            }

            return exportedCount;
        }
        finally
        {
            loader.Dispose();
        }
    }
}