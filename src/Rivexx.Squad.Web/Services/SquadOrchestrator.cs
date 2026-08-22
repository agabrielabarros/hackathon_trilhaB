using System.Text;
using System.Text.Json;
using Rivexx.Squad.Web.Domain;
using Rivexx.Squad.Web.Infrastructure;

namespace Rivexx.Squad.Web.Services;

public interface ISquadOrchestrator
{
    Task<SquadRun> RunAsync(string briefing, CancellationToken cancellationToken = default);
    Task<SquadRun?> GetLatestAsync(CancellationToken cancellationToken = default);
    Task<SquadRun?> GetAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class SquadOrchestrator(
    IAppStore store,
    IWebHostEnvironment environment,
    TimeProvider timeProvider,
    IAgentTeam agentTeam) : ISquadOrchestrator
{
    private readonly string _artifactRoot = Path.GetFullPath(
        Path.Combine(environment.ContentRootPath, "..", "..", "generated", "runs"));

    public async Task<SquadRun> RunAsync(string briefing, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(briefing) || briefing.Trim().Length < 40)
        {
            throw new ArgumentException("O briefing precisa descrever o contexto e o problema do cliente.", nameof(briefing));
        }

        var delivery = await agentTeam.ExecuteAsync(briefing.Trim(), cancellationToken);
        var now = timeProvider.GetUtcNow();
        var run = await store.MutateAsync(state =>
        {
            var item = new SquadRun
            {
                Id = state.Runs.Count == 0 ? 1 : state.Runs.Max(candidate => candidate.Id) + 1,
                Briefing = briefing.Trim(),
                Status = "Em execucao",
                StartedAt = now,
                ExecutionMode = delivery.ExecutionMode,
                Model = delivery.Model,
                Warnings = delivery.Warnings
            };
            AddEvent(item, "Sistema", "PO Agent", "briefing_received", "Briefing recebido", "concluido",
                new()
                {
                    ["summary"] = item.Briefing[..Math.Min(item.Briefing.Length, 240)],
                    ["executionMode"] = delivery.ExecutionMode,
                    ["model"] = delivery.Model
                });
            AddEvent(item, "PO Agent", "Squad", "backlog_created", "Backlog priorizado e criterios definidos", "concluido",
                new()
                {
                    ["stories"] = delivery.Plan.Stories.Select(story => story.Key).ToArray(),
                    ["source"] = delivery.Steps.First(step => step.Role == "PO Agent").Source
                });

            foreach (var definition in delivery.Plan.Stories.OrderBy(story => story.Priority))
            {
                var story = new UserStory
                {
                    Key = definition.Key,
                    Title = definition.Title,
                    Priority = definition.Priority,
                    Description = definition.Description,
                    AcceptanceCriteria = [.. definition.AcceptanceCriteria],
                    Status = "Em desenvolvimento"
                };
                item.Stories.Add(story);
                AddEvent(item, "PO Agent", "Dev Agent", "story_ready", $"{story.Key} pronta para desenvolvimento", "concluido",
                    new() { ["story"] = story.Key, ["criteria"] = story.AcceptanceCriteria });

                var implementation = delivery.Development.Implementations.First(candidate =>
                    candidate.StoryKey.Equals(story.Key, StringComparison.OrdinalIgnoreCase));
                var code = implementation.Code;
                item.Decisions.Add(new TechnicalDecision
                {
                    StoryKey = story.Key,
                    Title = story.Title,
                    Decision = implementation.Decision,
                    Rationale = implementation.Rationale,
                    CreatedAt = now
                });
                AddEvent(item, "Dev Agent", "QA Agent", "delivery_ready", $"{story.Key} implementada e enviada ao QA", "concluido",
                    new()
                    {
                        ["artifact"] = $"generated/runs/{item.Id}/features/{story.Key.ToLowerInvariant()}.cs",
                        ["decision"] = implementation.Decision,
                        ["source"] = delivery.Steps.First(step => step.Role == "Dev Agent").Source
                    });

                var assessment = delivery.Quality.Assessments.First(candidate =>
                    candidate.StoryKey.Equals(story.Key, StringComparison.OrdinalIgnoreCase));
                var qaResult = Validate(story, code, assessment, now);
                item.QaResults.Add(qaResult);
                story.Status = qaResult.Passed ? "Aceita" : "Reprovada";
                AddEvent(item, "QA Agent", "PO Agent", "quality_gate", $"{story.Key} {(qaResult.Passed ? "aprovada" : "reprovada")}",
                    qaResult.Passed ? "aprovado" : "reprovado",
                    new()
                    {
                        ["story"] = story.Key,
                        ["passed"] = qaResult.Passed,
                        ["evidence"] = qaResult.Evidence,
                        ["feedback"] = qaResult.Feedback,
                        ["source"] = delivery.Steps.First(step => step.Role == "QA Agent").Source
                    });
            }

            item.Status = item.QaResults.All(result => result.Passed) ? "Entregue" : "Com falhas";
            item.FinishedAt = now;
            AddEvent(item, "PO Agent", "Cliente", "release", "Release liberada com evidencias auditaveis",
                item.Status == "Entregue" ? "aprovado" : "reprovado",
                new() { ["status"] = item.Status, ["acceptedStories"] = item.QaResults.Count(result => result.Passed) });
            state.Runs.Add(item);
            return item;
        }, cancellationToken);

        await WriteArtifactsAsync(run, delivery.Development, cancellationToken);
        return run;
    }

    public Task<SquadRun?> GetLatestAsync(CancellationToken cancellationToken = default) =>
        store.ReadAsync(state => state.Runs.OrderByDescending(run => run.Id).FirstOrDefault(), cancellationToken);

    public Task<SquadRun?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        store.ReadAsync(state => state.Runs.FirstOrDefault(run => run.Id == id), cancellationToken);

    private static QaResult Validate(UserStory story, string code, QaAssessment assessment, DateTimeOffset now)
    {
        var cases = assessment.Cases
            .Select(test => new QaCase { Name = test.Name, Passed = test.Passed })
            .ToList();
        cases.Add(new QaCase { Name = "Hard gate: estrutura C# valida", Passed = HasBalancedBraces(code) });
        cases.Add(new QaCase { Name = "Hard gate: contrato publico", Passed = code.Contains("public static class", StringComparison.Ordinal) });
        var passed = assessment.Passed && cases.All(test => test.Passed);
        return new QaResult
        {
            StoryKey = story.Key,
            Passed = passed,
            Evidence = $"{cases.Count(test => test.Passed)}/{cases.Count} casos passaram",
            Feedback = assessment.Feedback,
            Cases = cases,
            CreatedAt = now
        };
    }

    private static bool HasBalancedBraces(string code)
    {
        var balance = 0;
        foreach (var character in code)
        {
            if (character == '{') balance++;
            if (character == '}' && --balance < 0) return false;
        }
        return balance == 0 && code.Contains("namespace Rivexx.Generated", StringComparison.Ordinal);
    }

    private static void AddEvent(
        SquadRun run, string sender, string recipient, string eventType,
        string title, string status, Dictionary<string, object?> payload) =>
        run.Events.Add(new AgentEvent
        {
            Sequence = run.Events.Count + 1,
            Sender = sender,
            Recipient = recipient,
            EventType = eventType,
            Title = title,
            Status = status,
            Payload = payload,
            CreatedAt = run.StartedAt
        });

    private async Task WriteArtifactsAsync(
        SquadRun run,
        DevOutput development,
        CancellationToken cancellationToken)
    {
        var runDirectory = Path.Combine(_artifactRoot, run.Id.ToString());
        var featureDirectory = Path.Combine(runDirectory, "features");
        Directory.CreateDirectory(featureDirectory);
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };

        await File.WriteAllTextAsync(Path.Combine(runDirectory, "backlog.json"),
            JsonSerializer.Serialize(run.Stories, jsonOptions), cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(runDirectory, "event-log.json"),
            JsonSerializer.Serialize(run.Events, jsonOptions), cancellationToken);

        var decisions = new StringBuilder("# Log de decisoes tecnicas\n\n");
        foreach (var decision in run.Decisions)
        {
            decisions.AppendLine($"## {decision.StoryKey} - {decision.Title}\n")
                .AppendLine($"**Decisao:** {decision.Decision}\n")
                .AppendLine($"**Justificativa:** {decision.Rationale}\n");
        }
        await File.WriteAllTextAsync(Path.Combine(runDirectory, "technical-decisions.md"), decisions.ToString(), cancellationToken);

        var report = new StringBuilder($"# Relatorio de QA - execucao {run.Id}\n\n");
        foreach (var result in run.QaResults)
        {
            report.AppendLine($"- {result.StoryKey}: {(result.Passed ? "APROVADA" : "REPROVADA")} - {result.Evidence}");
        }
        await File.WriteAllTextAsync(Path.Combine(runDirectory, "qa-report.md"), report.ToString(), cancellationToken);

        foreach (var story in run.Stories)
        {
            var implementation = development.Implementations.First(item =>
                item.StoryKey.Equals(story.Key, StringComparison.OrdinalIgnoreCase));
            await File.WriteAllTextAsync(Path.Combine(featureDirectory, $"{story.Key.ToLowerInvariant()}.cs"),
                implementation.Code, cancellationToken);
        }
    }
}
