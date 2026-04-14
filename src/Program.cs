using System.Collections.Immutable;
using System.Text.Json;
using Adp.Agent;
using Adp.Manifest;
using MyAdpAgent;

// ----------------------------------------------------------------------------
// MyAdpAgent template — forkable starter
// ----------------------------------------------------------------------------
// 1. Edit agents/example.json for your agent's identity, authorities, and
//    dissent conditions.
// 2. Edit src/MyEvaluator.cs to implement your actual decision logic —
//    this is where your agent casts votes.
// 3. Set ADP_BEARER_TOKEN and (optionally) ADP_PRIVATE_KEY / ADP_PUBLIC_KEY
//    in the environment before running.
// 4. dotnet run
// ----------------------------------------------------------------------------

var configPath = args.Length > 0 ? args[0] : "agents/example.json";
if (!File.Exists(configPath))
{
    Console.Error.WriteLine($"Config file not found: {configPath}");
    return 1;
}

var config = LoadConfig(configPath);
ApplyEnvOverrides(ref config);

var host = new AdpAgentHost(config, new AdpAgentHostOptions
{
    Evaluator = new MyEvaluator(config),
});

// Handle Ctrl-C / SIGTERM gracefully.
var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };
AppDomain.CurrentDomain.ProcessExit += (_, _) => shutdown.Cancel();

await host.StartAsync();

try
{
    await Task.Delay(Timeout.Infinite, shutdown.Token);
}
catch (OperationCanceledException)
{
    // expected on graceful shutdown
}

await host.StopAsync();
return 0;

// ----------------------------------------------------------------------------

static AgentConfig LoadConfig(string path)
{
    var json = File.ReadAllText(path);
    var raw = JsonSerializer.Deserialize<JsonElement>(json);

    string agentId = raw.GetProperty("agentId").GetString()!;
    int port = raw.GetProperty("port").GetInt32();
    string domain = raw.GetProperty("domain").GetString()!;

    var decisionClasses = raw.GetProperty("decisionClasses")
        .EnumerateArray()
        .Select(e => e.GetString()!)
        .ToImmutableList();

    var authorities = raw.GetProperty("authorities")
        .EnumerateObject()
        .ToImmutableDictionary(p => p.Name, p => p.Value.GetDouble());

    var dissentConditions = raw.GetProperty("dissentConditions")
        .EnumerateArray()
        .Select(e => e.GetString()!)
        .ToImmutableList();

    return new AgentConfig
    {
        AgentId = agentId,
        Port = port,
        Domain = domain,
        DecisionClasses = decisionClasses,
        Authorities = authorities,
        StakeMagnitude = Enum.Parse<StakeMagnitude>(
            raw.GetProperty("stakeMagnitude").GetString()!, ignoreCase: true),
        DefaultVote = Enum.Parse<Vote>(
            raw.GetProperty("defaultVote").GetString()!, ignoreCase: true),
        DefaultConfidence = raw.GetProperty("defaultConfidence").GetDouble(),
        DissentConditions = dissentConditions,
        JournalDir = raw.GetProperty("journalDir").GetString()!,
    };
}

static void ApplyEnvOverrides(ref AgentConfig config)
{
    var bearer = Environment.GetEnvironmentVariable("ADP_BEARER_TOKEN");
    var privateKey = Environment.GetEnvironmentVariable("ADP_PRIVATE_KEY");
    var publicKey = Environment.GetEnvironmentVariable("ADP_PUBLIC_KEY");

    if (bearer is not null || privateKey is not null || publicKey is not null)
    {
        config = config with
        {
            Auth = new AuthConfig
            {
                BearerToken = bearer ?? "",
                PrivateKey = privateKey,
                PublicKey = publicKey,
            },
        };
    }
}
