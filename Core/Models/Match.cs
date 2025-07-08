using BA_MU.Helpers;

namespace BA_MU.Core.Models;

public record Match(
    long ModdedId,
    long PatchId,
    string Name,
    string Type,
    int TypeId
)
{
    public string CleanName => FileManager.Clean(Name);
    public string JsonFileName => FileManager.CreateJsonName(Name, Type);
    public string DisplayName => $"{Name} ({Type})";
    public bool HasValidName => Name != "Unknown";
}