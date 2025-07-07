using BA_MU.Core.Utils;

namespace BA_MU.Core.Models;

public record Match(
    long ModdedId,
    long PatchId,
    string Name,
    string Type,
    int TypeId
)
{
    public string CleanName => FileName.Clean(Name);
    public string JsonFileName => FileName.CreateJsonFileName(Name, Type);
    public string DisplayName => $"{Name} ({Type})";
    public bool HasValidName => Name != "Unknown";
}