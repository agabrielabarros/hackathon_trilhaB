namespace Rivexx.Squad.Web.Domain;

public sealed class AppState
{
    public List<SquadRun> Runs { get; set; } = [];
    public List<Nonconformity> Nonconformities { get; set; } = [];
    public List<RootCauseAnalysis> RootCauseAnalyses { get; set; } = [];
    public List<ProductionLot> ProductionLots { get; set; } = [];
}

public sealed class SquadRun
{
    public int Id { get; set; }
    public required string Briefing { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string ExecutionMode { get; set; } = "Offline";
    public string? Model { get; set; }
    public List<string> Warnings { get; set; } = [];
    public List<AgentEvent> Events { get; set; } = [];
    public List<UserStory> Stories { get; set; } = [];
    public List<TechnicalDecision> Decisions { get; set; } = [];
    public List<QaResult> QaResults { get; set; } = [];
}

public sealed class AgentEvent
{
    public int Sequence { get; set; }
    public required string Sender { get; set; }
    public required string Recipient { get; set; }
    public required string EventType { get; set; }
    public required string Title { get; set; }
    public required string Status { get; set; }
    public required Dictionary<string, object?> Payload { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class UserStory
{
    public required string Key { get; set; }
    public required string Title { get; set; }
    public int Priority { get; set; }
    public required string Description { get; set; }
    public List<string> AcceptanceCriteria { get; set; } = [];
    public required string Status { get; set; }
}

public sealed class TechnicalDecision
{
    public required string StoryKey { get; set; }
    public required string Title { get; set; }
    public required string Decision { get; set; }
    public required string Rationale { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class QaResult
{
    public required string StoryKey { get; set; }
    public bool Passed { get; set; }
    public required string Evidence { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public List<QaCase> Cases { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class QaCase
{
    public required string Name { get; set; }
    public bool Passed { get; set; }
}

public sealed class Nonconformity
{
    public int Id { get; set; }
    public required string DefectType { get; set; }
    public required string Description { get; set; }
    public required string Line { get; set; }
    public required string LotCode { get; set; }
    public required string Responsible { get; set; }
    public required string Shift { get; set; }
    public required string Equipment { get; set; }
    public required string Evidence { get; set; }
    public string Status { get; set; } = "Aberta";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RootCauseAnalysis
{
    public int Id { get; set; }
    public int NonconformityId { get; set; }
    public required string Method { get; set; }
    public required string SuggestedCause { get; set; }
    public int Confidence { get; set; }
    public required string HistoricalEvidence { get; set; }
    public required string CorrectiveAction { get; set; }
    public required string Owner { get; set; }
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = "Pendente";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ProductionLot
{
    public required string LotCode { get; set; }
    public required string Product { get; set; }
    public required string MaterialLot { get; set; }
    public required string Supplier { get; set; }
    public required string Equipment { get; set; }
    public required string Shift { get; set; }
    public required string Operators { get; set; }
    public required string RelatedLots { get; set; }
    public required string Destination { get; set; }
    public required string Status { get; set; }
}

public sealed record BriefingRequest(string Briefing);

public sealed record NonconformityRequest(
    string DefectType,
    string Description,
    string Line,
    string LotCode,
    string Responsible,
    string Shift,
    string Equipment,
    string Evidence);

public sealed record DashboardMetrics(int Runs, int Events, int AcceptedStories, int Nonconformities);

public sealed record RootCauseView(
    RootCauseAnalysis Analysis,
    string DefectType,
    string LotCode,
    string Line);
