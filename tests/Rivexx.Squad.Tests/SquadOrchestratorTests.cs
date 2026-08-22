using Rivexx.Squad.Web.Services;

namespace Rivexx.Squad.Tests;

public sealed class SquadOrchestratorTests
{
    private const string Briefing = "A Rivexx precisa registrar nao conformidades, analisar causa raiz e rastrear lotes em uma aplicacao responsiva e auditavel para os tres turnos.";

    [Fact]
    public async Task DeliversThreeStoriesWithVisibleAuditTrail()
    {
        using var workspace = new TemporaryWorkspace();
        var store = new FakeStore();
        var orchestrator = CreateOrchestrator(store, workspace);

        var run = await orchestrator.RunAsync(Briefing);

        Assert.Equal("Entregue", run.Status);
        Assert.Equal(3, run.Stories.Count);
        Assert.All(run.Stories, story => Assert.Equal("Aceita", story.Status));
        Assert.Equal(3, run.QaResults.Count);
        Assert.All(run.QaResults, result => Assert.True(result.Passed));
        Assert.Equal(12, run.Events.Count);
        Assert.Equal("Sistema", run.Events[0].Sender);
        Assert.Equal("Cliente", run.Events[^1].Recipient);
        Assert.Equal("Offline", run.ExecutionMode);
        Assert.Equal(3, run.Warnings.Count);
    }

    [Fact]
    public async Task WritesRequiredDeliveryArtifacts()
    {
        using var workspace = new TemporaryWorkspace();
        var orchestrator = CreateOrchestrator(new FakeStore(), workspace);

        var run = await orchestrator.RunAsync(Briefing);
        var runDirectory = Path.Combine(workspace.Root, "generated", "runs", run.Id.ToString());

        Assert.True(File.Exists(Path.Combine(runDirectory, "backlog.json")));
        Assert.True(File.Exists(Path.Combine(runDirectory, "technical-decisions.md")));
        Assert.True(File.Exists(Path.Combine(runDirectory, "qa-report.md")));
        Assert.Equal(3, Directory.GetFiles(Path.Combine(runDirectory, "features"), "*.cs").Length);
    }

    [Fact]
    public async Task RejectsIncompleteBriefing()
    {
        using var workspace = new TemporaryWorkspace();
        var orchestrator = CreateOrchestrator(new FakeStore(), workspace);

        await Assert.ThrowsAsync<ArgumentException>(() => orchestrator.RunAsync("briefing curto"));
    }

    private static SquadOrchestrator CreateOrchestrator(FakeStore store, TemporaryWorkspace workspace) =>
        new(store, new TestEnvironment(workspace.WebRoot), new TestTimeProvider(), new AgentTeam(new DisabledLlmClient()));
}
