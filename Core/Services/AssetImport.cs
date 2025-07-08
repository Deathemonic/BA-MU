using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BA_MU.Bundle;
using BA_MU.Core.Models;
using BA_MU.Helpers;
using BA_MU.ImportExport.Assets;

namespace BA_MU.Core.Services;

public class AssetImport
{
    public async Task<int> ImportAssets(Load loader, List<Match> matches)
    {
        if (!ValidateSetup())
            return 0;

        var assetsFileInstance = loader.GetAssetsFileInstance();
        if (assetsFileInstance == null)
        {
            Logs.Error("Failed to get assets file instance for import");
            return 0;
        }

        var refManager = CreateRefManager(assetsFileInstance);

        Logs.Info("Importing JSON assets...");

        var importedCount = await ProcessMatches(matches, assetsFileInstance, loader.GetAssetsManager(), refManager);

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

    private static RefTypeManager CreateRefManager(AssetsFileInstance assetsFileInstance)
    {
        var refManager = new RefTypeManager();
        refManager.FromTypeTree(assetsFileInstance.file.Metadata);
        return refManager;
    }

    private static async Task<int> ProcessMatches(List<Match> matches, AssetsFileInstance assetsFileInstance, AssetsManager assetsManager, RefTypeManager refManager)
    {
        var importedCount = 0;

        foreach (var match in matches)
            try
            {
                if (await ProcessSingleMatch(match, assetsFileInstance, assetsManager, refManager)) importedCount++;
            }
            catch (Exception ex)
            {
                Logs.Error($"Error importing asset {match.PatchId}", ex);
            }

        return importedCount;
    }

    private static async Task<bool> ProcessSingleMatch(Match match, AssetsFileInstance assetsFileInstance, AssetsManager assetsManager, RefTypeManager refManager)
    {
        var targetAssetInfo = assetsFileInstance.file.AssetInfos.FirstOrDefault(a => a.PathId == match.PatchId);
        if (targetAssetInfo == null)
        {
            Logs.Error($"Asset with PathID {match.PatchId} not found in target bundle");
            return false;
        }

        var jsonFilePath = Path.Combine(FileManager.GetDumpPath(), match.JsonFileName);
        if (!File.Exists(jsonFilePath))
        {
            Logs.Error($"JSON file not found: {match.JsonFileName}");
            return false;
        }

        var templateField = GetTemplateField(assetsManager, assetsFileInstance, targetAssetInfo, match.Name);
        if (templateField == null)
            return false;

        Logs.Debug($"Processing: {match.Name} ({match.Type})");

        var importedData = await ImportJsonData(jsonFilePath, templateField, refManager, match.Name);
        if (importedData == null)
            return false;

        targetAssetInfo.SetNewData(importedData);
        Logs.Debug($"Imported: {match.Name} ({match.Type})");
        return true;
    }

    private static AssetTypeTemplateField? GetTemplateField(AssetsManager assetsManager, AssetsFileInstance assetsFileInstance, AssetFileInfo targetAssetInfo, string assetName)
    {
        var templateField = assetsManager.GetTemplateBaseField(assetsFileInstance, targetAssetInfo);
        if (templateField != null)
            return templateField;

        Logs.Error($"Failed to get template field for asset {assetName}");
        return null;
    }

    private static async Task<byte[]?> ImportJsonData(string jsonFilePath, AssetTypeTemplateField templateField, RefTypeManager refManager, string assetName)
    {
        try
        {
            await using var jsonStream = File.OpenRead(jsonFilePath);
            var importer = new Import(jsonStream, refManager);
            var importedData = importer.ImportJsonAsset(templateField, out var exceptionMessage);

            if (importedData != null)
                return importedData;

            Logs.Error($"Failed to import JSON for {assetName}: {exceptionMessage}");
            return null;
        }
        catch (Exception ex)
        {
            Logs.Error($"Error reading JSON file for {assetName}", ex);
            return null;
        }
    }
}