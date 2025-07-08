using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BA_MU.Bundle;
using BA_MU.Core.Models;
using BA_MU.Helpers;
using BA_MU.ImportExport.Assets;

namespace BA_MU.Core.Services;

public class AssetExport
{
    public async Task<int> ExportAssets(string moddedPath, List<Match> matches)
    {
        FileManager.DumpDirExists();

        var loader = new Load();

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

        Logs.Info("Exporting JSON dumps...");

        var exportedCount = await ProcessMatches(matches, assetsFileInstance, loader.GetAssetsManager());

        return exportedCount;
    }

    private static async Task<int> ProcessMatches(List<Match> matches, AssetsFileInstance assetsFileInstance, AssetsManager assetsManager)
    {
        var exportedCount = 0;

        foreach (var match in matches)
            try
            {
                if (await ProcessSingleMatch(match, assetsFileInstance, assetsManager)) exportedCount++;
            }
            catch (Exception ex)
            {
                Logs.Error($"Error exporting asset {match.ModdedId}", ex);
            }

        return exportedCount;
    }

    private static async Task<bool> ProcessSingleMatch(Match match, AssetsFileInstance assetsFileInstance, AssetsManager assetsManager)
    {
        var assetInfo = assetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.ModdedId);
        if (assetInfo == null)
        {
            Logs.Error($"Asset with PathId {match.ModdedId} not found in modded bundle");
            return false;
        }

        var baseField = GetBaseField(assetsManager, assetsFileInstance, assetInfo, match.ModdedId);
        if (baseField == null)
            return false;

        var filePath = FileManager.GetFilePath(FileManager.GetDumpPath(), match.JsonFileName);

        await ExportJsonData(baseField, filePath);

        Logs.Debug($"Exported: {match.Name} ({match.Type})");
        return true;
    }


    private static AssetTypeValueField? GetBaseField(AssetsManager assetsManager, AssetsFileInstance assetsFileInstance, AssetFileInfo assetInfo, long assetId)
    {
        var baseField = assetsManager.GetBaseField(assetsFileInstance, assetInfo);
        if (baseField != null)
            return baseField;

        Logs.Error($"Failed to get base field for asset {assetId}");
        return null;
    }

    private static async Task ExportJsonData(AssetTypeValueField baseField, string filePath)
    {
        await using var fileStream = File.Create(filePath);
        var exporter = new Export(fileStream);
        exporter.DumpJsonAsset(baseField);
    }
}