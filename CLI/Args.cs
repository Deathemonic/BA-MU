using BA_MU.Helpers;

namespace BA_MU.CLI;

public static class Args
{
    /// <summary>
    /// Blue Archive - Mod Updater
    /// </summary>
    /// <param name="modded">-m, Path to the modded asset bundle.</param>
    /// <param name="patch">-p, Path to the patch asset bundle.</param>
    /// <param name="includeTypes">--include, Comma-separated list of asset types to include (e.g., "gameobject,transform").</param>
    /// <param name="excludeTypes">--exclude, Comma-separated list of asset types to exclude (e.g., "gameobject,transform").</param>
    /// <param name="onlyTypes">--only, Only allow these asset types to match (e.g., "mesh,texture2d").</param>
    /// <param name="verbose">-v, Enable verbose debug output.</param>
    public static void Run(string modded, string patch, string? includeTypes = null, string? excludeTypes = null, string? onlyTypes = null, bool verbose = false)
    {
        Logs.SetVerbose(verbose);
        _ = Parse.Execute(modded, patch, includeTypes, excludeTypes, onlyTypes);
    }
}