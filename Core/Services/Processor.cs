using AssetsTools.NET.Texture;
using BA_MU.Bundle;
using BA_MU.Core.Models;
using BA_MU.Core.Utils;
using BA_MU.Helpers;

namespace BA_MU.Core.Services;

public class Processor(Comparison comparison, AssetExport assetExport, AssetImport assetImport, TextureExport textureExport)
{
    public async Task ProcessBundles(string moddedPath, string patchPath, Options options, ImageExportType exportType)
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

        var textureMatches = matches.Where(m => m.Type.Equals("Texture2D", StringComparison.OrdinalIgnoreCase)).ToList();
        var otherMatches = matches.Where(m => !m.Type.Equals("Texture2D", StringComparison.OrdinalIgnoreCase)).ToList();

        var exportedCount = 0;
        var textureExportCount = 0;

        if (otherMatches.Count != 0)
        {
            exportedCount = await assetExport.ExportAssets(moddedPath, otherMatches);
        }

        if (textureMatches.Count != 0)
        {
            textureExportCount = await textureExport.ExportTextures(moddedPath, textureMatches, exportType);
        }
        
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

            var importedCount = await assetImport.ImportAssets(loader, otherMatches);
            
            if (importedCount > 0)
            {
                Save.SaveModdedBundle(loader, patchPath);
            }

            if (exportedCount > 0)
            {
                Logs.Success($"Successfully exported {exportedCount} regular assets to {FileName.GetDumpsDirectory()}");
            }

            if (textureExportCount > 0)
            {
                Logs.Success($"Successfully exported {textureExportCount} textures to {FileName.GetDumpsDirectory()}");
            }

            if (importedCount > 0)
            {
                Logs.Success($"Successfully imported {importedCount} assets from {FileName.GetDumpsDirectory()}");
                Logs.Info($"{importedCount} assets have been marked as modified and will be saved");
            }
            else if (exportedCount == 0 && textureExportCount == 0)
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