using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using BA_MU.Bundle;
using BA_MU.Core.Models;
using BA_MU.Helpers;
using BA_MU.ImportExport.Texture2D;

namespace BA_MU.Core.Services;

public class TextureExport
{
    public Task<int> ExportTextures(string moddedPath, List<Match> matches,
        ImageExportType exportType = ImageExportType.Tga)
    {
        FileManager.DumpDirExists();

        if (matches.Count == 0)
        {
            Logs.Warn("No Texture2D assets to export");
            return Task.FromResult(0);
        }

        var loader = new Load();

        if (!loader.LoadBundle(moddedPath))
        {
            Logs.Error("Failed to load modded bundle for texture export");
            return Task.FromResult(0);
        }

        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logs.Error("Failed to get assets file instance for texture export");
            return Task.FromResult(0);
        }

        Logs.Info($"Exporting Texture2D assets as {exportType}...");

        var exportedCount = ProcessMatches(matches, exportType, assetsFileInstance, loader.GetAssetsManager());

        return Task.FromResult(exportedCount);
    }

    private static int ProcessMatches(List<Match> matches, ImageExportType exportType,
        AssetsFileInstance assetsFileInstance, AssetsManager assetsManager)
    {
        var exportedCount = 0;

        foreach (var match in matches)
            try
            {
                if (ProcessSingleMatch(match, exportType, assetsFileInstance, assetsManager)) exportedCount++;
            }
            catch (Exception ex)
            {
                Logs.Error($"Error exporting texture {match.ModdedId}", ex);
            }

        return exportedCount;
    }

    private static bool ProcessSingleMatch(Match match, ImageExportType exportType,
        AssetsFileInstance assetsFileInstance, AssetsManager assetsManager)
    {
        var assetInfo = assetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.ModdedId);
        if (assetInfo == null)
        {
            Logs.Error($"Texture2D asset with PathId {match.ModdedId} not found in modded bundle");
            return false;
        }

        var filePath = BuildFilePath(match.Name, exportType);

        Logs.Debug($"Attempting to export texture: {match.Name} (TypeId: {match.TypeId}, PathId: {match.ModdedId})");

        var success = Export.ExportTexture(assetsFileInstance, assetsManager, assetInfo, filePath, exportType);

        if (!success)
        {
            Logs.Error($"Failed to export texture: {match.Name}");
            return false;
        }

        var fileName = Path.GetFileName(filePath);
        Logs.Debug($"Exported texture: {match.Name} -> {fileName}");
        return true;
    }

    private static string BuildFilePath(string assetName, ImageExportType exportType)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var extension = exportType == ImageExportType.Png ? "png" : "tga";
        var fileName = $"{cleanAssetName}.{extension}";
        return FileManager.GetFilePath(FileManager.GetDumpPath(), fileName);
    }
}