using AssetsTools.NET.Extra;
using BA_MU.Bundle;
using BA_MU.Core.Models;
using BA_MU.Helpers;
using BA_MU.ImportExport.Text;


namespace BA_MU.Core.Services;

public class TextExport
{
    public Task<int> ExportTextAssets(string moddedPath, List<Match> matches, string textFormat)
    {
        FileManager.DumpDirExists();

        if (matches.Count == 0)
        {
            Logs.Warn("No TextAsset assets to export");
            return Task.FromResult(0);
        }

        var loader = new Load();

        if (!loader.LoadBundle(moddedPath))
        {
            Logs.Error("Failed to load modded bundle for text export");
            return Task.FromResult(0);
        }

        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logs.Error("Failed to get assets file instance for text export");
            return Task.FromResult(0);
        }

        Logs.Info("Exporting TextAsset assets...");

        var exportedCount = ProcessMatches(matches, assetsFileInstance, loader.GetAssetsManager(), textFormat);

        return Task.FromResult(exportedCount);
    }

    private static int ProcessMatches(List<Match> matches, AssetsFileInstance assetsFileInstance, AssetsManager assetsManager, string textFormat)
    {
        var exportedCount = 0;

        foreach (var match in matches)
            try
            {
                if (ProcessSingleMatch(match, assetsFileInstance, assetsManager, textFormat)) exportedCount++;
            }
            catch (Exception ex)
            {
                Logs.Error($"Error exporting text asset {match.ModdedId}", ex);
            }

        return exportedCount;
    }

    private static bool ProcessSingleMatch(Match match, AssetsFileInstance assetsFileInstance, AssetsManager assetsManager, string textFormat)
    {
        var assetInfo = assetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.ModdedId);
        if (assetInfo == null)
        {
            Logs.Error($"TextAsset with PathId {match.ModdedId} not found in modded bundle");
            return false;
        }

        var filePath = BuildFilePath(match.Name, textFormat);

        Logs.Debug($"Attempting to export text asset: {match.Name} (TypeId: {match.TypeId}, PathId: {match.ModdedId})");

        var success = Export.ExportTextAsset(assetsFileInstance, assetsManager, assetInfo, filePath);

        if (!success)
        {
            Logs.Error($"Failed to export text asset: {match.Name}");
            return false;
        }

        var fileName = Path.GetFileName(filePath);
        Logs.Debug($"Exported text asset: {match.Name} -> {fileName}");
        return true;
    }

    private static string BuildFilePath(string assetName, string textFormat)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var fileName = $"{cleanAssetName}.{textFormat}";
        return FileManager.GetFilePath(FileManager.GetDumpPath(), fileName);
    }
}