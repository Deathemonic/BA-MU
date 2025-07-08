using AssetsTools.NET.Extra;
using BA_MU.Bundle;
using BA_MU.Core.Models;
using BA_MU.Helpers;
using BA_MU.ImportExport.Texture2D;

namespace BA_MU.Core.Services;

public class TextureImport
{
    public async Task<int> ImportTextures(Load loader, List<Match> matches)
    {
        if (!ValidateSetup())
            return 0;

        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logs.Error("Failed to get assets file instance for import");
            return 0;
        }

        Logs.Info("Importing texture assets...");

        var importedCount = await ProcessMatches(matches, assetsFileInstance, loader.GetAssetsManager());

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

    private static async Task<int> ProcessMatches(List<Match> matches, AssetsFileInstance assetsFileInstance,
        AssetsManager assetsManager)
    {
        var importedCount = 0;

        foreach (var match in matches)
            try
            {
                if (await ProcessSingleMatch(match, assetsFileInstance, assetsManager)) importedCount++;
            }
            catch (Exception ex)
            {
                Logs.Error($"Error importing texture {match.PatchId}", ex);
            }

        return importedCount;
    }

    private static async Task<bool> ProcessSingleMatch(Match match, AssetsFileInstance assetsFileInstance,
        AssetsManager assetsManager)
    {
        var targetAssetInfo = assetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.PatchId);
        if (targetAssetInfo == null)
        {
            Logs.Error($"Asset with PathID {match.PatchId} not found in target bundle");
            return false;
        }

        var filePath = FindTextureFile(match.Name);
        if (filePath == null)
        {
            Logs.Error($"Texture file not found for: {FileManager.Clean(match.Name)}");
            return false;
        }

        Logs.Debug($"Processing texture: {match.Name}");

        var success = await Import.ImportTexture(assetsFileInstance, assetsManager, targetAssetInfo, filePath);

        if (!success)
        {
            Logs.Error($"Failed to import texture for {match.Name}");
            return false;
        }

        Logs.Debug($"Imported texture: {match.Name}");
        return true;
    }

    private static string? FindTextureFile(string assetName)
    {
        var cleanAssetName = FileManager.Clean(assetName);
        var dumpsDir = FileManager.GetDumpPath();

        var candidates = new[]
        {
            Path.Combine(dumpsDir, $"{cleanAssetName}.png"),
            Path.Combine(dumpsDir, $"{cleanAssetName}.tga")
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}