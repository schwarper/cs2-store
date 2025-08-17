using System.Text.Json;
using CounterStrikeSharp.API;

namespace Store;

public static class Log
{
    public enum LogType
    {
        GiveCredit,
        GiftCredit
    }

    public class LogEntry
    {
        public string Date { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string FromId { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string ToId { get; set; } = string.Empty;
        public int CreditsGiven { get; set; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void SaveLog(
        string fromName,
        string fromSteamId,
        string toName,
        string toSteamId,
        int creditsAmount,
        LogType logType)
    {
        try
        {
            string logFolder = Path.Combine(
                Server.GameDirectory,
                "csgo",
                "addons",
                "counterstrikesharp",
                "logs",
                "Store"
            );

            Directory.CreateDirectory(logFolder);

            string logFile = Path.Combine(
                logFolder,
                $"{DateTime.Now:dd.MM.yyyy}-{logType.ToString().ToLower()}-log.json"
            );

            List<LogEntry> logs = LoadLogs(logFile);

            LogEntry logEntry = new()
            {
                Date = DateTime.Now.ToString("HH:mm:ss"),
                From = fromName,
                FromId = fromSteamId,
                To = toName,
                ToId = toSteamId,
                CreditsGiven = creditsAmount
            };

            logs.Add(logEntry);

            File.WriteAllText(logFile, JsonSerializer.Serialize(logs, JsonOptions));
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"credit log error: {ex.Message}");
        }
    }

    private static List<LogEntry> LoadLogs(string logFile)
    {
        if (!File.Exists(logFile))
            return [];

        try
        {
            string existing = File.ReadAllText(logFile);
            return string.IsNullOrWhiteSpace(existing)
                ? []
                : JsonSerializer.Deserialize<List<LogEntry>>(existing) ?? [];
        }
        catch
        {
            return [];
        }
    }
}