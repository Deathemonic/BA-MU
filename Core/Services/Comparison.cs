using BA_MU.Bundle;
using BA_MU.Core.Models;
using BA_MU.Core.Utils;
using ZLinq;

namespace BA_MU.Core.Services;

public class Comparison
{
    public List<Match> FindMatches(string moddedPath, string patchPath, Options options)
    {
        var moddedLoader = new Load();
        var patchLoader = new Load();

        try
        {
            if (!moddedLoader.LoadBundle(moddedPath) || !patchLoader.LoadBundle(patchPath))
                return [];

            return CompareAssets(moddedLoader, patchLoader, options);
        }
        finally
        {
            moddedLoader.Dispose();
            patchLoader.Dispose();
        }
    }

    private static List<Match> CompareAssets(Load moddedLoader, Load patchLoader, Options options)
    {
        try
        {
            var moddedAssets = GetAssetInfo(moddedLoader);
            var patchAssets = GetAssetInfo(patchLoader);

            return patchAssets
                .AsValueEnumerable()
                .Where(p =>
                    moddedAssets.AsValueEnumerable().Any(m =>
                        m.Value.Name == p.Value.Name &&
                        m.Value.Type == p.Value.Type))
                .Where(p => !options.ShouldFilterAsset(p.Value.Type.ToLowerInvariant(), p.Value.Name))
                .Select(p => new Match(
                    moddedAssets.AsValueEnumerable().First(m =>
                        m.Value.Name == p.Value.Name &&
                        m.Value.Type == p.Value.Type).Key,
                    p.Key,
                    p.Value.Name,
                    p.Value.Type,
                    p.Value.TypeId))
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error comparing assets: {ex.Message}");
            return [];
        }
    }

    private static Dictionary<long, (string Name, string Type, int TypeId)> GetAssetInfo(Load loader)
    {
        var assets = new Dictionary<long, (string Name, string Type, int TypeId)>();
        var assetsFileInstance = loader.GetAssetsFileInstance();

        if (assetsFileInstance == null)
            return assets;

        foreach (var assetInfo in assetsFileInstance.file.AssetInfos)
        {
            var baseField = loader.GetAssetsManager().GetBaseField(assetsFileInstance, assetInfo);
            var assetName = "Unknown";
            var assetType = TypeMapper.GetAssetTypeName(assetInfo.TypeId);

            var nameField = baseField?["m_Name"];
            if (nameField is { IsDummy: false })
                assetName = nameField.AsString;

            assets[assetInfo.PathId] = (assetName, assetType, assetInfo.TypeId);
        }

        return assets;
    }
}