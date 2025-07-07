using AssetsTools.NET.Texture;
using BA_MU.Bundle;
using BA_MU.ImportExport.Texture2D;
using BA_MU.Core.Models;
using BA_MU.Core.Utils;
using BA_MU.Helpers;

namespace BA_MU.Core.Services;

public class TextureExport
{
    public async Task<int> ExportTextures(
        string moddedPath, 
        List<Match> matches, 
        ImageExportType exportType = ImageExportType.Tga)
    {
        FileName.EnsureDumpsDirectoryExists();

        if (matches.Count == 0)
        {
            Logs.Warn("No Texture2D assets to export");
            return 0;
        }

        var loader = new Load();
        try
        {
            if (!loader.LoadBundle(moddedPath))
            {
                Logs.Error("Failed to load modded bundle for texture export");
                return 0;
            }

            var assetsFileInstance = loader.GetAssetsFileInstance();
            if (assetsFileInstance == null)
            {
                Logs.Error("Failed to get assets file instance for texture export");
                return 0;
            }

            var assetsManager = loader.GetAssetsManager();
            var exportedCount = 0;

            Logs.Info($"Exporting Texture2D assets as {exportType}...");

            foreach (var match in matches)
            {
                try
                {
                    var assetInfo = assetsFileInstance.file.AssetInfos
                        .FirstOrDefault(a => a.PathId == match.ModdedId);
                        
                    if (assetInfo == null)
                    {
                        Logs.Error($"Texture2D asset with PathId {match.ModdedId} not found in modded bundle");
                        continue;
                    }

                    var cleanAssetName = FileName.Clean(match.Name);
                    var extension = exportType == ImageExportType.Png ? "png" : "tga";
                    var fileName = $"{cleanAssetName}.{extension}";
                    var filePath = FileName.GetUniqueFilePath(FileName.GetDumpsDirectory(), fileName);

                    Logs.Debug($"Attempting to export texture: {match.Name} (TypeId: {match.TypeId}, PathId: {match.ModdedId})");

                    var success = await Export.ExportSingle(
                        assetsFileInstance, 
                        assetsManager, 
                        assetInfo, 
                        filePath, 
                        exportType
                    );

                    if (success)
                    {
                        exportedCount++;
                        Logs.Debug($"Exported texture: {match.Name} -> {fileName}");
                    }
                    else
                    {
                        Logs.Error($"Failed to export texture: {match.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Logs.Error($"Error exporting texture {match.ModdedId}", ex);
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