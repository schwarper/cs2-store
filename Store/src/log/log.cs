using CounterStrikeSharp.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Store
{
    public static class Log
    {
        public enum LogType
        {
            GiveCredit,
            GiftCredit
        }

        public static void SaveLog(string fromName, string fromSteamId, string toName, string toSteamId, int creditsAmount, LogType logType)
        {
            try
            {
                string logFolder = Path.Combine(Server.GameDirectory, "csgo", "addons", "counterstrikesharp", "logs", "Store");

                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);

                string today = DateTime.Now.ToString("dd.MM.yyyy");
                string logFile = Path.Combine(logFolder, $"{today}-{logType.ToString().ToLower()}-log.json");

                List<object> logs = new List<object>();

                if (File.Exists(logFile))
                {
                    string existing = File.ReadAllText(logFile);
                    if (!string.IsNullOrWhiteSpace(existing))
                    {
                        try
                        {
                            logs = JsonSerializer.Deserialize<List<object>>(existing) ?? new List<object>();
                        }
                        catch
                        {
                            logs = new List<object>();
                        }
                    }
                }

                var logEntry = new
                {
                    Date = DateTime.Now.ToString("HH:mm:ss"),
                    From = fromName,
                    FromId = fromSteamId,
                    To = toName,
                    ToId = toSteamId,
                    CreditsGiven = creditsAmount
                };

                logs.Add(logEntry);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                File.WriteAllText(logFile, JsonSerializer.Serialize(logs, options));
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"credit log error: {ex.Message}");
            }
        }
    }
}
