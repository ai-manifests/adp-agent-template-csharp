# adp-agent-template-csharp

A forkable .NET 10 starter for building an [Agent Deliberation Protocol](https://adp-manifest.dev) agent on the C# / ASP.NET Core runtime. Clone this, edit `agents/example.json` and `src/MyEvaluator.cs`, and you have a federation-ready agent.

Depends on [`Adp.Agent`](https://github.com/ai-manifests/adp-agent-csharp) from the Gitea NuGet feed.

## 30-second quickstart

```bash
# 1. Clone
git clone https://github.com/ai-manifests/adp-agent-template-csharp.git my-adp-agent
cd my-adp-agent

# 2. Generate a bearer token
export ADP_BEARER_TOKEN=$(openssl rand -hex 32)

# 3. Restore + run
dotnet run -- agents/example.json
```

Visit `http://localhost:3000/healthz` — you should see `{"status":"ok","agentId":"did:adp:my-agent-v1"}`.

Also try:
- `http://localhost:3000/.well-known/adp-manifest.json`
- `http://localhost:3000/.well-known/adp-calibration.json` (503 until you configure a signing key — see below)

## What to edit

### 1. `agents/example.json` — your agent's identity

```json
{
  "agentId": "did:adp:my-agent-v1",
  "port": 3000,
  "domain": "my-agent.example.com",
  "decisionClasses": ["code.correctness"],
  "authorities": { "code.correctness": 0.7 },
  "stakeMagnitude": "medium",
  "defaultVote": "approve",
  "defaultConfidence": 0.65,
  "dissentConditions": [
    "if any test marked critical regresses"
  ],
  "journalDir": "./journal"
}
```

Rename the file and update `Program.cs` to point at the new path if you want multiple configs side by side.

### 2. `src/MyEvaluator.cs` — your decision logic

This is the only file where you write real code. Every time the agent receives `POST /api/propose`, the runtime calls `MyEvaluator.EvaluateAsync` with the action, decision class, and reversibility tier. Your job is to return an `EvaluationResult` — a vote, a confidence in `[0, 1]`, and a rationale.

The stub approves everything at the configured default confidence. Replace it with something real:

```csharp
public async ValueTask<EvaluationResult> EvaluateAsync(
    EvaluationRequest request, CancellationToken ct = default)
{
    var exitCode = await RunTestsAsync(request.Action.Target, ct);
    if (exitCode == 0)
        return EvaluationResult.Approve(confidence: 0.85, rationale: "All tests pass");
    return EvaluationResult.Reject(confidence: 0.85, rationale: $"Tests failed: exit {exitCode}");
}
```

### 3. Signed calibration snapshots (optional but strongly recommended)

Generate an Ed25519 key pair and set it via env vars. Any Ed25519-capable keygen works; if you have the Adp.Agent library available at a REPL:

```csharp
var (pub, priv) = Adp.Agent.Signing.Ed25519Signer.GenerateKeyPair();
Console.WriteLine($"ADP_PRIVATE_KEY={priv}");
Console.WriteLine($"ADP_PUBLIC_KEY={pub}");
```

Copy the two values into your environment and restart. `/.well-known/adp-calibration.json` will start returning a signed snapshot covering every declared decision class.

### 4. Optional: Neo3 chain anchoring

Edit `MyAdpAgent.csproj` to uncomment the `Adp.Agent.Anchor` package reference, then wire up a `CalibrationAnchorScheduler` inside `Program.cs`:

```csharp
using Adp.Agent.Anchor;

// ... after building the host ...
if (config.CalibrationAnchor is { Enabled: true })
{
    var store = BlockchainStoreFactory.Create(config.CalibrationAnchor);
    if (store is not null)
    {
        var scheduler = new CalibrationAnchorScheduler(config, host.Journal, store);
        host.AfterStart(() => { scheduler.Start(); return Task.CompletedTask; });
        host.BeforeStop(async () => await scheduler.StopAsync());
    }
}
```

Targets: `mock`, `neo-express`, `neo-custom`, `neo-testnet`, `neo-mainnet`. Same code, same smart contract, only the RPC URL and signing wallet change.

## Docker

A multi-stage Dockerfile and a docker-compose.yml are included for production deployment.

```bash
# Build + run
docker compose up --build

# Or build the image directly
docker build -t my-adp-agent:latest .
docker run -p 3000:3000 \
  -e ADP_BEARER_TOKEN=$(openssl rand -hex 32) \
  -v $(pwd)/journal:/app/journal \
  my-adp-agent:latest
```

## NuGet feed

`Adp.Agent` is published to the `ai-manifests` Gitea NuGet feed. The `nuget.config` at the repo root routes `Adp.Agent`, `Adj.Manifest`, `Adp.Manifest`, and `Acb.Manifest` to Gitea and everything else to nuget.org. No extra auth configuration needed — reads are anonymous.

## Federation checklist

Once your agent is running locally, to join a real federation:

1. Put your agent behind HTTPS at a stable domain (the `domain` field in your config must resolve)
2. Share the URL to `/.well-known/adp-manifest.json` with the registry or with peers
3. Implement a real `MyEvaluator` that reflects genuine expertise — agents that fake calibration lose weight fast
4. Watch the registry's audit flag your agent's self-reported calibration against the recomputed value. If the two match, you're calibrated. If they diverge, your agent is lying and will lose weight over time.

## License

Apache-2.0 — see [`LICENSE`](LICENSE) for the full license text and [`NOTICE`](NOTICE) for attribution. Fork freely; your own agent code (the parts you add on top of the template) is yours to license however you want, as long as the original `LICENSE` and `NOTICE` files remain with the template content you redistribute unchanged (per Apache-2.0 §4).
