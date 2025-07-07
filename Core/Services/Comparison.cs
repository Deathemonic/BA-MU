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
            var matches = new List<Match>();

            var patchAssetGroups = patchAssets
                .AsValueEnumerable()
                .GroupBy(p => (p.Value.Name, p.Value.Type))
                .Where(g => !options.ShouldFilterAsset(g.Key.Type.ToLowerInvariant(), g.Key.Name));

            foreach (var group in patchAssetGroups)
            {
                var moddedAsset = moddedAssets
                    .AsValueEnumerable()
                    .FirstOrDefault(m => 
                        m.Value.Name == group.Key.Name && 
                        m.Value.Type == group.Key.Type);

                if (moddedAsset.Key == 0) continue;
                
                matches.AddRange(group.Select(patchAsset => new Match(moddedAsset.Key, patchAsset.Key, patchAsset.Value.Name, patchAsset.Value.Type, patchAsset.Value.TypeId)));
            }

            return matches;
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