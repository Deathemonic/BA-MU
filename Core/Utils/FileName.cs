namespace BA_MU.Core.Utils;

public static class FileName
{
    public static string Clean(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }

    public static string CreateJsonFileName(string assetName, string assetType)
    {
        var cleanName = Clean(assetName);
        return $"{cleanName}_{assetType}.json";
    }

    public static string GetUniqueFilePath(string directory, string fileName)
    {
        var filePath = Path.Combine(directory, fileName);
        var counter = 1;
        
        while (File.Exists(filePath))
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var newFileName = $"{nameWithoutExtension}_{counter}{extension}";
            filePath = Path.Combine(directory, newFileName);
            counter++;
        }

        return filePath;
    }

    public static string GetDumpsDirectory()
    {
        return Path.Combine(Directory.GetCurrentDirectory(), "Dumps");
    }

    public static void EnsureDumpsDirectoryExists()
    {
        var dumpsDir = GetDumpsDirectory();
        Directory.CreateDirectory(dumpsDir);
    }
}