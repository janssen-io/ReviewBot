using System;
using System.IO;
using System.Linq;
using System.Text.Json;

var ikey = Environment.GetEnvironmentVariable("AI_IKEY");
var botId = Environment.GetEnvironmentVariable("BOT_ID");
var botSecret = Environment.GetEnvironmentVariable("BOT_SECRET");
var botToken = Environment.GetEnvironmentVariable("BOT_REFRESHTOKEN");
var dbConn = Environment.GetEnvironmentVariable("LITEDB_CONNECTIONSTRING");

var serializedSecrets = JsonSerializer.Serialize(new
{
    ApplicationInsights = new
    {
        InstrumentationKey = ikey
    },
    ReviewBot = new
    {
        AppId = botId,
        RefreshToken = botToken,
        AppSecret = botSecret
    },
    Store = new
    {
        ConnectionString = dbConn
    }
});

var path = Environment.GetCommandLineArgs()
    .Skip(2) // skip dotnet-script and script name
    .FirstOrDefault() ?? "secrets.json";

Console.WriteLine("Writing secrets to: " + path);
File.WriteAllText(path, serializedSecrets);
