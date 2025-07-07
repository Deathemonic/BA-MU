using BA_MU.Bundle;
using BA_MU.Core.Models;
using BA_MU.Core.Utils;
using BA_MU.Helpers;

namespace BA_MU.Core.Services;

public class Processor(Comparison comparison, AssetExport assetExport, AssetImport assetImport)
{
    public async Task ProcessBundles(string moddedPath, string patchPath, Options options)
    {
        var matches = comparison.FindMatches(moddedPath, patchPath, options);
        
        if (matches.Count == 0)
        {
            Logs.Warn("No matching assets found");
            return;
        }

        Logs.Info($"Found {matches.Count} matching assets");
        Logs.Info("Matching Assets:");

        foreach (var match in matches)
        {
            Logs.Info($"  {match.DisplayName} - PathID: {match.ModdedId}");
        }
        
        var exportedCount = await assetExport.ExportAssets(moddedPath, matches);
        
        var loader = new Load();
        try
        {
            if (!loader.LoadBundle(patchPath))
            {
                Logs.Error("Failed to load patch bundle for import");
                return;
            }

            if (!DatabaseLoader.LoadClassDatabase(loader.GetAssetsManager()))
            {
                Logs.Error("Failed to load class database");
                return;
            }

            var importedCount = await assetImport.ImportAssets(loader, matches);
            
            if (importedCount > 0)
            {
                Save.SaveModdedBundle(loader, patchPath);
            }

            if (exportedCount > 0)
            {
                Logs.Success($"Successfully exported {exportedCount} assets to {FileName.GetDumpsDirectory()}");
            }

            if (importedCount > 0)
            {
                Logs.Success($"Successfully imported {importedCount} assets from {FileName.GetDumpsDirectory()}");
                Logs.Info($"{importedCount} assets have been marked as modified and will be saved");
            }
            else if (exportedCount == 0)
            {
                Logs.Warn("No assets were processed");
            }
        }
        finally
        {
            loader.Dispose();
        }
    }

    public async Task OverwriteBundle(string originalPath, List<Match> matches)
    {
        var dumpsDir = FileName.GetDumpsDirectory();
        if (!Directory.Exists(dumpsDir))
        {
            Logs.Error("Dumps directory not found. Please run parse command first");
            return;
        }

        var jsonFiles = Directory.GetFiles(dumpsDir, "*.json");
        if (jsonFiles.Length == 0)
        {
            Logs.Error("No JSON files found in dumps directory");
            return;
        }

        Logs.Info($"Found {jsonFiles.Length} JSON files in dumps directory");
        Logs.Info($"Applying changes to: {Path.GetFileName(originalPath)}");

        var loader = new Load();
        try
        {
            if (!loader.LoadBundle(originalPath))
            {
                Logs.Error("Failed to load bundle for modification");
                return;
            }

            if (!DatabaseLoader.LoadClassDatabase(loader.GetAssetsManager()))
            {
                Logs.Error("Failed to load class database");
                return;
            }

            var importedCount = await assetImport.ImportAssets(loader, matches);
            
            if (importedCount > 0)
            {
                Save.SaveModdedBundle(loader, originalPath);
            }

            if (importedCount > 0)
            {
                Logs.Success($"Successfully imported {importedCount} assets from {dumpsDir}");
                Logs.Info($"{importedCount} assets have been marked as modified and will be saved");
            }
            else
            {
                Logs.Warn("No assets were processed");
            }
        }
        finally
        {
            loader.Dispose();
        }
    }
}