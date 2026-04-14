using Adp.Agent;
using Adp.Manifest;

namespace MyAdpAgent;

/// <summary>
/// REPLACE THIS — this is where your agent's actual decision logic goes.
///
/// Every time the agent receives a proposal request on <c>POST /api/propose</c>,
/// the runtime hands an <see cref="EvaluationRequest"/> to this class and
/// expects back an <see cref="EvaluationResult"/> — your vote, your
/// confidence, and an optional rationale and evidence links.
///
/// The stub below approves everything at the agent's configured default
/// confidence. Replace it with something real:
///
///   - Run your test suite and vote based on pass/fail
///   - Query a database or API for signals
///   - Call an LLM and parse its response
///   - Inspect a git commit, a PR diff, a build artifact
///   - Whatever your agent is an expert at
///
/// What matters is that <see cref="Vote"/> and <see cref="double"/> confidence
/// in [0, 1] honestly reflect your belief about the proposed action. Downstream
/// calibration scoring (Brier score) grades your honesty over time — agents
/// that are well-calibrated earn weight, agents that overclaim lose weight.
/// </summary>
public sealed class MyEvaluator : IEvaluator
{
    private readonly AgentConfig _config;

    public MyEvaluator(AgentConfig config)
    {
        _config = config;
    }

    public ValueTask<EvaluationResult> EvaluateAsync(
        EvaluationRequest request,
        CancellationToken ct = default)
    {
        // TODO: REPLACE THIS STUB.
        //
        // Example: run tests and vote based on exit code.
        //
        //   var exitCode = await RunTestsAsync(request.Action.Target, ct);
        //   if (exitCode == 0)
        //       return EvaluationResult.Approve(confidence: 0.85, rationale: "All tests pass");
        //   return EvaluationResult.Reject(confidence: 0.85, rationale: $"Tests failed: exit {exitCode}");

        var result = new EvaluationResult(
            Vote: _config.DefaultVote,
            Confidence: _config.DefaultConfidence,
            Rationale: "stub evaluator — replace MyEvaluator.EvaluateAsync with real decision logic"
        );

        return ValueTask.FromResult(result);
    }
}
