using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BA_MU.Helpers;

namespace BA_MU.Bundle;

public static class Save
{
    public static void SaveModdedBundle(Load patchLoader, string originalPatchPath)
    {
        try
        {
            var bundleFileInstance = patchLoader.GetBundleInstance();
            var assetsFileInstance = patchLoader.GetAssetsFileInstance();
            
            if (bundleFileInstance == null || assetsFileInstance == null)
            {
                Logs.Error("Could not get bundle or assets file instance for saving");
                return;
            }

            var moddedFolderPath = Path.Combine(FileManager.GetModdedPath());
            Directory.CreateDirectory(moddedFolderPath);

            var originalFileName = Path.GetFileName(originalPatchPath);
            var outputPath = Path.Combine(moddedFolderPath, originalFileName);

            var replacerCount = CountReplacers(assetsFileInstance);
            
            if (replacerCount == 0)
            {
                Logs.Warn("No modifications detected in assets file");
                return;
            }

            Logs.Info($"Saving {replacerCount} modified assets...");

            var dirInfo = bundleFileInstance.file.BlockAndDirInfo.DirectoryInfos
                .FirstOrDefault(d => !d.Name.EndsWith(".resS"));

            if (dirInfo == null)
            {
                Logs.Error("Could not find main directory in bundle");
                return;
            }

            using var tempStream = new MemoryStream();
            using var tempWriter = new AssetsFileWriter(tempStream);
            
            assetsFileInstance.file.Write(tempWriter);
            
            dirInfo.SetNewData(tempStream.ToArray());

            using var finalWriter = new AssetsFileWriter(outputPath);
            bundleFileInstance.file.Write(finalWriter);

            Logs.Success($"Successfully saved modded bundle to: {outputPath}");
            Logs.Info($"Applied {replacerCount} asset modifications");
        }
        catch (Exception ex)
        {
            Logs.Error("Error saving modded bundle", ex);
        }
    }

    private static int CountReplacers(AssetsFileInstance assetsFileInstance)
    {
        var count = 0;
        
        foreach (var assetInfo in assetsFileInstance.file.AssetInfos)
        {
            if (assetInfo.Replacer != null)
            {
                count++;
            }
        }
        return count;
    }
}