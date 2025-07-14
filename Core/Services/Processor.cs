using AssetsTools.NET.Texture;
using BA_MU.Bundle;
using BA_MU.Core.Models;
using BA_MU.Core.Utils;
using BA_MU.Helpers;

namespace BA_MU.Core.Services;

public class Processor(Comparison comparison, AssetExport assetExport, AssetImport assetImport, TextureExport textureExport, TextureImport textureImport, TextExport textExport, TextImport textImport)
{
    public async Task ProcessBundles(string moddedPath, string patchPath, Options options, ImageExportType exportType)
    {
        PrepareDirectories();

        var matches = comparison.FindMatches(moddedPath, patchPath, options);
        if (matches.Count == 0)
        {
            Logs.Warn("No matching assets found");
            return;
        }

        LogMatchingAssets(matches);

        var (textureMatches, textAssetMatches, otherMatches) = CategorizeMatches(matches);
        var exportResults = await PerformExports(moddedPath, textureMatches, textAssetMatches, otherMatches, exportType, options.TextFormat);
        await PerformImports(patchPath, textureMatches, textAssetMatches, otherMatches, exportResults);
    }

    private static void PrepareDirectories()
    {
        Logs.Info("Preparing directories...");
        FileManager.CleanupDirectories();
        Logs.Debug("Cleaned up existing Dumps and Modded directories");
    }

    private static void LogMatchingAssets(List<Match> matches)
    {
        Logs.Success($"Found {matches.Count} matching assets");
        Logs.Info("Matching Assets:");

        foreach (var match in matches)
        {
            Logs.Info($"{match.DisplayName} - PathID: {match.ModdedId}");
        }
    }

    private static (List<Match> textureMatches, List<Match> textAssetMatches, List<Match> otherMatches) CategorizeMatches(List<Match> matches)
    {
        var textureMatches = matches.Where(m => m.Type.Equals("Texture2D", StringComparison.OrdinalIgnoreCase)).ToList();
        var textAssetMatches = matches.Where(m => m.Type.Equals("TextAsset", StringComparison.OrdinalIgnoreCase)).ToList();
        var otherMatches = matches.Where(m => 
            !m.Type.Equals("Texture2D", StringComparison.OrdinalIgnoreCase) && 
            !m.Type.Equals("TextAsset", StringComparison.OrdinalIgnoreCase)).ToList();

        return (textureMatches, textAssetMatches, otherMatches);
    }

    private async Task<ExportResults> PerformExports(string moddedPath, List<Match> textureMatches, List<Match> textAssetMatches, List<Match> otherMatches, ImageExportType exportType, string textFormat)
    {
        var exportedCount = 0;
        var textureExportCount = 0;
        var textAssetExportCount = 0;

        if (otherMatches.Count > 0)
        {
            exportedCount = await assetExport.ExportAssets(moddedPath, otherMatches);
        }

        if (textureMatches.Count > 0)
        {
            textureExportCount = await textureExport.ExportTextures(moddedPath, textureMatches, exportType);
        }

        if (textAssetMatches.Count > 0)
        {
            textAssetExportCount = await textExport.ExportTextAssets(moddedPath, textAssetMatches, textFormat);
        }

        return new ExportResults(exportedCount, textureExportCount, textAssetExportCount);
    }

    private async Task PerformImports(string patchPath, List<Match> textureMatches, List<Match> textAssetMatches, List<Match> otherMatches, ExportResults exportResults)
    {
        var loader = new Load();

        if (!SetupLoader(loader, patchPath))
            return;

        var importResults = await ExecuteImports(loader, textureMatches, textAssetMatches, otherMatches);

        SaveChanges(loader, patchPath, importResults);

        LogResults(exportResults, importResults);
    }

    private static bool SetupLoader(Load loader, string patchPath)
    {
        if (!loader.LoadBundle(patchPath))
        {
            Logs.Error("Failed to load patch bundle for import");
            return false;
        }

        if (DatabaseLoader.LoadClassDatabase(loader.GetAssetsManager()))
            return true;

        Logs.Error("Failed to load class database");
        return false;
    }

    private async Task<ImportResults> ExecuteImports(Load loader, List<Match> textureMatches, List<Match> textAssetMatches, List<Match> otherMatches)
    {
        var importedCount = 0;
        var textureImportCount = 0;
        var textAssetImportCount = 0;
        
        if (otherMatches.Count > 0)
        {
            importedCount = await assetImport.ImportAssets(loader, otherMatches);
        }

        if (textureMatches.Count > 0)
        { 
            textureImportCount = await textureImport.ImportTextures(loader, textureMatches);
        }

        if (textAssetMatches.Count > 0)
        { 
            textAssetImportCount = await textImport.ImportTextAssets(loader, textAssetMatches);
        }

        return new ImportResults(importedCount, textureImportCount, textAssetImportCount);
    }

    private static void SaveChanges(Load loader, string patchPath, ImportResults importResults)
    {
        if (importResults.TotalImported > 0)
        {
            Save.SaveModdedBundle(loader, patchPath);
        }
    }

    private static void LogResults(ExportResults exportResults, ImportResults importResults)
    {
        LogExportResults(exportResults);
        LogImportResults(importResults);
        LogFinalStatus(exportResults, importResults);
    }

    private static void LogExportResults(ExportResults results)
    {
        if (results.ExportedCount > 0)
        {
            Logs.Success($"Successfully exported {results.ExportedCount} assets to {FileManager.GetDumpPath()}");
        }

        if (results.TextureExportCount > 0)
        {
            Logs.Success($"Successfully exported {results.TextureExportCount} textures to {FileManager.GetDumpPath()}");
        }

        if (results.TextAssetExportCount > 0)
        {
            Logs.Success($"Successfully exported {results.TextAssetExportCount} text assets to {FileManager.GetDumpPath()}");
        }
    }

    private static void LogImportResults(ImportResults results)
    {
        if (results.ImportedCount > 0)
        {
            Logs.Success($"Successfully imported {results.ImportedCount} assets");
        }

        if (results.ImportedTextureCount > 0)
        {
            Logs.Success($"Successfully imported {results.ImportedTextureCount} textures");
        }

        if (results.ImportedTextAssetCount > 0)
        {
            Logs.Success($"Successfully imported {results.ImportedTextAssetCount} text assets");
        }

        if (results.TotalImported > 0)
        {
            Logs.Success($"{results.TotalImported} assets have been marked as modified and will be saved");
        }
    }

    private static void LogFinalStatus(ExportResults exportResults, ImportResults importResults)
    {
        if (importResults.TotalImported == 0 && exportResults.TotalExported == 0)
        {
            Logs.Warn("No assets were processed");
        }
    }

    private record ExportResults(int ExportedCount, int TextureExportCount, int TextAssetExportCount)
    {
        public int TotalExported => ExportedCount + TextureExportCount + TextAssetExportCount;
    }

    private record ImportResults(int ImportedCount, int ImportedTextureCount, int ImportedTextAssetCount)
    {
        public int TotalImported => ImportedCount + ImportedTextureCount + ImportedTextAssetCount;
    }
}