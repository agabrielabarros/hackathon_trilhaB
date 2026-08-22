using System.Text.Json;

namespace Rivexx.Squad.Web.Services;

public sealed record PoStory(
    string Key,
    string Title,
    int Priority,
    string Description,
    List<string> AcceptanceCriteria);

public sealed record PoOutput(string Summary, List<PoStory> Stories);

public sealed record DevImplementation(
    string StoryKey,
    string Code,
    string Decision,
    string Rationale);

public sealed record DevOutput(List<DevImplementation> Implementations);

public sealed record QaCaseOutput(string Name, bool Passed);

public sealed record QaAssessment(
    string StoryKey,
    bool Passed,
    string Evidence,
    string Feedback,
    List<QaCaseOutput> Cases);

public sealed record QaOutput(List<QaAssessment> Assessments);

public sealed record AgentStepInfo(
    string Role,
    string Source,
    string Model,
    string? ResponseId,
    string? Warning);

public sealed record AgentTeamDelivery(
    PoOutput Plan,
    DevOutput Development,
    QaOutput Quality,
    string ExecutionMode,
    string? Model,
    List<string> Warnings,
    List<AgentStepInfo> Steps);

public interface IAgentTeam
{
    Task<AgentTeamDelivery> ExecuteAsync(string briefing, CancellationToken cancellationToken = default);
}

public sealed class AgentTeam(ILlmClient llmClient) : IAgentTeam
{
    private static readonly HashSet<string> RequiredStoryKeys =
        new(["US-01", "US-02", "US-03"], StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<AgentTeamDelivery> ExecuteAsync(
        string briefing,
        CancellationToken cancellationToken = default)
    {
        var steps = new List<AgentStepInfo>();

        var poCall = await llmClient.GenerateAsync<PoOutput>(
            "po_backlog",
            AgentSchemas.Po,
            """
            Voce e o PO Agent de um squad autonomo. Receba somente o briefing do cliente e converta-o em exatamente
            tres user stories priorizadas: registro agil de nao conformidade, causa raiz assistida e rastreabilidade
            de lote. Cada story deve ter pelo menos tres criterios observaveis. Nao invente integracoes externas,
            nao faca perguntas e nao inclua texto fora do schema.
            """,
            briefing,
            cancellationToken);
        var plan = IsValid(poCall.Value) ? poCall.Value! : OfflinePlan();
        steps.Add(Step("PO Agent", poCall, IsValid(poCall.Value)));

        var devInput = JsonSerializer.Serialize(new { briefing, backlog = plan }, JsonOptions);
        var devCall = await llmClient.GenerateAsync<DevOutput>(
            "dev_delivery",
            AgentSchemas.Dev,
            """
            Voce e o Dev Agent. Consuma somente o briefing e o backlog aprovados pelo PO. Para cada story, produza
            um modulo C# pequeno, seguro e autocontido, alem de uma decisao tecnica e sua justificativa. Nao gere
            comandos de shell, acesso a segredos, rede, processos ou escrita de arquivos. Use o namespace
            Rivexx.Generated e devolva exatamente uma implementacao por story, sem texto fora do schema.
            """,
            devInput,
            cancellationToken);
        var development = IsValid(devCall.Value, plan) ? devCall.Value! : OfflineDevelopment(plan);
        steps.Add(Step("Dev Agent", devCall, IsValid(devCall.Value, plan)));

        var qaInput = JsonSerializer.Serialize(new { briefing, backlog = plan, delivery = development }, JsonOptions);
        var qaCall = await llmClient.GenerateAsync<QaOutput>(
            "qa_report",
            AgentSchemas.Qa,
            """
            Voce e o QA Agent e possui autoridade de bloquear a release. Avalie cada implementacao contra os
            criterios de aceite correspondentes. Crie pelo menos tres casos objetivos por story, registre evidencia
            curta e feedback acionavel. Aprove somente quando todos os seus casos passarem. Nao altere codigo e nao
            inclua texto fora do schema.
            """,
            qaInput,
            cancellationToken);
        var quality = IsValid(qaCall.Value, plan) ? qaCall.Value! : OfflineQuality(plan, development);
        steps.Add(Step("QA Agent", qaCall, IsValid(qaCall.Value, plan)));

        var llmSteps = steps.Count(step => step.Source == "OpenAI");
        var mode = llmSteps == steps.Count ? "OpenAI" : llmSteps == 0 ? "Offline" : "Hibrido";
        var warnings = steps.Where(step => step.Warning is not null).Select(step => step.Warning!).ToList();
        return new AgentTeamDelivery(
            plan,
            development,
            quality,
            mode,
            llmSteps > 0 ? llmClient.Model : null,
            warnings,
            steps);
    }

    private static AgentStepInfo Step<T>(string role, LlmCallResult<T> call, bool accepted) =>
        new(
            role,
            accepted && call.Succeeded ? "OpenAI" : "Fallback offline",
            call.Model,
            accepted ? call.ResponseId : null,
            accepted && call.Succeeded ? null : $"{role}: {call.Error ?? "saida rejeitada pela validacao semantica"}");

    private static bool IsValid(PoOutput? output) =>
        output is { Stories.Count: 3 }
        && RequiredStoryKeys.SetEquals(output.Stories.Select(story => story.Key))
        && output.Stories.All(story => !string.IsNullOrWhiteSpace(story.Key)
            && !string.IsNullOrWhiteSpace(story.Title)
            && story.AcceptanceCriteria.Count >= 3);

    private static bool IsValid(DevOutput? output, PoOutput plan) =>
        output is not null
        && output.Implementations.Count == plan.Stories.Count
        && plan.Stories.All(story => output.Implementations.Any(item =>
            item.StoryKey.Equals(story.Key, StringComparison.OrdinalIgnoreCase)
            && item.Code.Length >= 80
            && item.Code.Contains("namespace Rivexx.Generated", StringComparison.Ordinal)));

    private static bool IsValid(QaOutput? output, PoOutput plan) =>
        output is not null
        && output.Assessments.Count == plan.Stories.Count
        && plan.Stories.All(story => output.Assessments.Any(item =>
            item.StoryKey.Equals(story.Key, StringComparison.OrdinalIgnoreCase)
            && item.Cases.Count >= 3
            && item.Passed == item.Cases.All(test => test.Passed)));

    private static PoOutput OfflinePlan() => new(
        "Centralizar nao conformidades, tornar a causa raiz explicavel e responder por lotes em segundos.",
        StoryCatalog.All.Select(story => new PoStory(
            story.Key,
            story.Title,
            story.Priority,
            story.Description,
            [.. story.AcceptanceCriteria])).ToList());

    private static DevOutput OfflineDevelopment(PoOutput plan) => new(
        plan.Stories.Select(story =>
        {
            var definition = StoryCatalog.All.FirstOrDefault(item => item.Key == story.Key);
            return new DevImplementation(
                story.Key,
                FeatureCodeGenerator.Generate(story.Key),
                definition?.Decision ?? "Implementar um modulo C# autocontido e auditavel.",
                definition?.Rationale ?? "Reduz dependencias e mantem o comportamento verificavel.");
        }).ToList());

    private static QaOutput OfflineQuality(PoOutput plan, DevOutput development) => new(
        plan.Stories.Select(story =>
        {
            var implementation = development.Implementations.First(item => item.StoryKey == story.Key);
            var cases = new List<QaCaseOutput>
            {
                new("Namespace de codigo gerado presente", implementation.Code.Contains("namespace Rivexx.Generated", StringComparison.Ordinal)),
                new("Criterios de aceite presentes", story.AcceptanceCriteria.Count >= 3),
                new("Feature possui contrato publico", implementation.Code.Contains("public static class", StringComparison.Ordinal))
            };
            var passed = cases.All(test => test.Passed);
            return new QaAssessment(
                story.Key,
                passed,
                $"{cases.Count(test => test.Passed)}/{cases.Count} casos passaram",
                passed ? "Story pronta para release." : "Corrigir os casos reprovados antes da release.",
                cases);
        }).ToList());
}

internal static class AgentSchemas
{
    private static object StringProperty() => new { type = "string" };
    private static object BooleanProperty() => new { type = "boolean" };

    public static object Po => new
    {
        type = "object",
        additionalProperties = false,
        properties = new
        {
            summary = StringProperty(),
            stories = new
            {
                type = "array",
                minItems = 3,
                maxItems = 3,
                items = new
                {
                    type = "object",
                    additionalProperties = false,
                    properties = new
                    {
                        key = StringProperty(),
                        title = StringProperty(),
                        priority = new { type = "integer" },
                        description = StringProperty(),
                        acceptanceCriteria = new { type = "array", minItems = 3, items = StringProperty() }
                    },
                    required = new[] { "key", "title", "priority", "description", "acceptanceCriteria" }
                }
            }
        },
        required = new[] { "summary", "stories" }
    };

    public static object Dev => new
    {
        type = "object",
        additionalProperties = false,
        properties = new
        {
            implementations = new
            {
                type = "array",
                minItems = 3,
                maxItems = 3,
                items = new
                {
                    type = "object",
                    additionalProperties = false,
                    properties = new
                    {
                        storyKey = StringProperty(),
                        code = StringProperty(),
                        decision = StringProperty(),
                        rationale = StringProperty()
                    },
                    required = new[] { "storyKey", "code", "decision", "rationale" }
                }
            }
        },
        required = new[] { "implementations" }
    };

    public static object Qa => new
    {
        type = "object",
        additionalProperties = false,
        properties = new
        {
            assessments = new
            {
                type = "array",
                minItems = 3,
                maxItems = 3,
                items = new
                {
                    type = "object",
                    additionalProperties = false,
                    properties = new
                    {
                        storyKey = StringProperty(),
                        passed = BooleanProperty(),
                        evidence = StringProperty(),
                        feedback = StringProperty(),
                        cases = new
                        {
                            type = "array",
                            minItems = 3,
                            items = new
                            {
                                type = "object",
                                additionalProperties = false,
                                properties = new { name = StringProperty(), passed = BooleanProperty() },
                                required = new[] { "name", "passed" }
                            }
                        }
                    },
                    required = new[] { "storyKey", "passed", "evidence", "feedback", "cases" }
                }
            }
        },
        required = new[] { "assessments" }
    };
}
