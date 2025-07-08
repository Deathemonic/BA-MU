using AssetsTools.NET.Extra;
using System.Reflection;

using BA_MU.Helpers;


namespace BA_MU.Core.Utils;

public static class DatabaseLoader
{
    private static string? _tempClassDataPath;

    public static bool LoadClassDatabase(AssetsManager assetsManager)
    {
        try
        {
            Logs.Info("Starting class database loading...");
            
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "BA_MU.TPK.classdata.tpk";
            
            Logs.Debug($"Looking for embedded resource: {resourceName}");
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Logs.Error($"Embedded resource not found: {resourceName}");
                return false;
            }

            _tempClassDataPath = Path.Combine(Path.GetTempPath(), $"classdata_{Guid.NewGuid()}.tpk");
            
            Logs.Debug($"Extracting resource to temporary file: {_tempClassDataPath}");
            
            using (var fileStream = File.Create(_tempClassDataPath))
            {
                stream.CopyTo(fileStream);
            }

            Logs.Debug("Loading class package into AssetsManager...");
            assetsManager.LoadClassPackage(_tempClassDataPath);
            
            Logs.Success("Class database loaded successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logs.Error("Failed to load class database", ex);
            return false;
        }
    }
}