using BA_MU.Core.Models;
using BA_MU.Core.Services;
using BA_MU.Helpers;

namespace BA_MU.CLI;

public static class Parse
{
    public static async Task Execute(string modded, string patch, string? includeTypes = null,
        string? excludeTypes = null, string? onlyTypes = null)
    {
        if (string.IsNullOrEmpty(modded) || string.IsNullOrEmpty(patch))
        {
            Logs.Error("Both modded and patch bundle paths are required");
            return;
        }
        
        var options = Options.FromStrings(includeTypes, excludeTypes, onlyTypes);

        var comparison = new Comparison();
        var assetExport = new AssetExport();
        var assetImport = new AssetImport();
        var processor = new Processor(comparison, assetExport, assetImport);

        await processor.ProcessBundles(modded, patch, options);
    }
}