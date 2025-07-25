﻿using AssetsTools.NET.Texture;

using BA_MU.Core.Models;
using BA_MU.Core.Services;
using BA_MU.Helpers;


namespace BA_MU.CLI;

public static class Parse
{
    public static async Task Execute(
        string modded, 
        string patch, 
        string? includeTypes = null,
        string? excludeTypes = null, 
        string? onlyTypes = null,
        string imageFormat = "tga",
        string textFormat = "txt")
    {
        if (string.IsNullOrEmpty(modded) || string.IsNullOrEmpty(patch))
        {
            Logs.Error("Both modded and patch bundle paths are required");
            return;
        }
        
        var options = Options.FromStrings(includeTypes, excludeTypes, onlyTypes);
        options = options with { TextFormat = textFormat };
        var exportFormat = imageFormat.Equals("png", StringComparison.InvariantCultureIgnoreCase) ? 
            ImageExportType.Png : ImageExportType.Tga;

        var comparison = new Comparison();
        var assetExport = new AssetExport();
        var assetImport = new AssetImport();
        var textureExport = new TextureExport();
        var textureImport = new TextureImport();
        var textExport = new TextExport();
        var textImport = new TextImport();
        var processor = new Processor(comparison, assetExport, assetImport, textureExport, textureImport, textExport, textImport);

        await processor.ProcessBundles(modded, patch, options, exportFormat);
    }
}