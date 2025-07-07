namespace BA_MU.Core.Models;

public record Options(
    HashSet<string>? IncludeTypes = null,
    HashSet<string>? ExcludeTypes = null,
    HashSet<string>? OnlyTypes = null
)
{
    private static readonly HashSet<string> DefaultExclusions = new(StringComparer.OrdinalIgnoreCase)
    {
        "gameobject", "transform", "monobehaviour"
    };
    
    public bool ShouldFilterAsset(string assetType, string assetName)
    {
        var lowerAssetType = assetType.ToLowerInvariant();
        
        if (assetName == "Unknown")
            return true;
        
        if (OnlyTypes is { Count: > 0 })
            return !OnlyTypes.Contains(lowerAssetType);
        
        if (IncludeTypes is { Count: > 0 })
            return !IncludeTypes.Contains(lowerAssetType);
        
        if (ExcludeTypes is { Count: > 0 })
            return ExcludeTypes.Contains(lowerAssetType);
        
        return DefaultExclusions.Contains(lowerAssetType);
    }
    
    public static Options FromStrings(string? includeTypes, string? excludeTypes, string? onlyTypes)
    {
        return new Options(
            IncludeTypes: includeTypes?.Split(',').Select(t => t.Trim().ToLowerInvariant()).ToHashSet(),
            ExcludeTypes: excludeTypes?.Split(',').Select(t => t.Trim().ToLowerInvariant()).ToHashSet(),
            OnlyTypes: onlyTypes?.Split(',').Select(t => t.Trim().ToLowerInvariant()).ToHashSet()
        );
    }
}