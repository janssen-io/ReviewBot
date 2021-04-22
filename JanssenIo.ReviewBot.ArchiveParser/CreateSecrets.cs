using System;
using System.IO;
using System.Linq;
using System.Text.Json;

var ikey = Environment.GetEnvironmentVariable("AI_IKEY");
var dbConn = Environment.GetEnvironmentVariable("LITEDB_CONNECTIONSTRING");

var serializedSecrets = JsonSerializer.Serialize(new
{
    ApplicationInsights = new
    {
        InstrumentationKey = ikey
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
