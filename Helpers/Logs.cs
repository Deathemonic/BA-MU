using Kokuban;

namespace BA_MU.Helpers;

public static class Logs
{
    private static bool _verboseEnabled;

    public static void SetVerbose(bool enabled)
    {
        _verboseEnabled = enabled;
    }

    public static void Info(string message)
    {
        Console.WriteLine(Chalk.Blue + "[INFO] " + message);
    }

    public static void Warn(string message)
    {
        Console.WriteLine(Chalk.Yellow + "[WARN] " + message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        Console.WriteLine(Chalk.Red + "[ERROR] " + message);
        if (ex != null)
        {
            Console.WriteLine(Chalk.Red + $"Exception: {ex.Message}");
        }
    }

    public static void Debug(string message)
    {
        if (!_verboseEnabled) return;
        Console.WriteLine(Chalk.Cyan + "[DEBUG] " + message);
    }

    public static void Success(string message)
    {
        Console.WriteLine(Chalk.Green + "[SUCCESS] " + message);
    }
}