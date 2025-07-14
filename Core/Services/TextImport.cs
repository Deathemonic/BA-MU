using AssetsTools.NET.Extra;
using BA_MU.Bundle;
using BA_MU.Core.Models;
using BA_MU.Helpers;
using BA_MU.ImportExport.Text;

namespace BA_MU.Core.Services;

public class TextImport
{
    public async Task<int> ImportTextAssets(Load loader, List<Match> matches)
    {
        if (!ValidateSetup())
            return 0;

        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logs.Error("Failed to get assets file instance for import");
            return 0;
        }

        Logs.Info("Importing text assets...");

        var assetsManager = loader.GetAssetsManager();
        var importedCount = await ProcessMatches(matches, assetsFileInstance, assetsManager);

        return importedCount;
    }

    private static bool ValidateSetup()
    {
        var dumpsDir = FileManager.GetDumpPath();
        if (Directory.Exists(dumpsDir))
            return true;

        Logs.Error("Dumps directory not found. Please run parse command first");
        return false;
    }

    private static async Task<int> ProcessMatches(List<Match> matches, AssetsFileInstance assetsFileInstance, AssetsManager assetsManager)
    {
        var importedCount = 0;

        foreach (var match in matches)
            try
            {
                if (await ProcessSingleMatch(match, assetsFileInstance, assetsManager))
                    importedCount++;
            }
            catch (Exception ex)
            {
                Logs.Error($"Error importing text asset {match.PatchId}", ex);
            }

        return importedCount;
    }

    private static Task<bool> ProcessSingleMatch(Match match, AssetsFileInstance assetsFileInstance, AssetsManager assetsManager)
    {
        var targetAssetInfo = assetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.PatchId);
        if (targetAssetInfo == null)
        {
            Logs.Error($"Asset with PathID {match.PatchId} not found in target bundle");
            return Task.FromResult(false);
        }

        var filePath = FindTextFile(match.Name);
            if (filePath == null)
            {
                Logs.Error($"Text file not found for: {FileManager.Clean(match.Name)}");
                return Task.FromResult(false);
            }

        Logs.Debug($"Processing text asset: {match.Name}");

        var success = Import.ImportTextAsset(assetsFileInstance, assetsManager, targetAssetInfo, filePath);

        if (!success)
        {
            Logs.Error($"Failed to import text asset for {match.Name}");
            return Task.FromResult(false);
        }

        Logs.Debug($"Imported text asset: {match.Name}");
        return Task.FromResult(true);
    }

    private static string? FindTextFile(string assetName)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var dumpsDir = FileManager.GetDumpPath();

        var candidates = new[]
        {
            Path.Combine(dumpsDir, $"{cleanAssetName}.txt"),
            Path.Combine(dumpsDir, $"{cleanAssetName}.bytes")
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}