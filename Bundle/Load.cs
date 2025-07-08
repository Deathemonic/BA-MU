using AssetsTools.NET.Extra;


namespace BA_MU.Bundle;

public class Load
{
    private BundleFileInstance? _bundleInstance;
    private readonly AssetsManager _assetsManager = new();

    public bool LoadBundle(string path)
    {
        if (!File.Exists(path))
            return false;

        _bundleInstance = _assetsManager.LoadBundleFile(path);
        return _bundleInstance != null;
    }
    
    public BundleFileInstance? GetBundleInstance()
    {
        return _bundleInstance;
    }

    public AssetsManager GetAssetsManager()
    {
        return _assetsManager;
    }

    public AssetsFileInstance? GetAssetsFileInstance()
    {
        var cabFile = _bundleInstance?.file.BlockAndDirInfo.DirectoryInfos
            .FirstOrDefault(d => !d.Name.EndsWith(".resS"));

        return cabFile == null ? null : _assetsManager.LoadAssetsFileFromBundle(_bundleInstance, cabFile.Name);
    }

    public void Dispose()
    {
        _bundleInstance?.file.Close();
        _assetsManager.UnloadAll();
    }
}