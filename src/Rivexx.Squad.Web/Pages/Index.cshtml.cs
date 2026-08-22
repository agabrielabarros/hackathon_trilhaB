using Microsoft.AspNetCore.Mvc.RazorPages;
using Rivexx.Squad.Web.Domain;
using Rivexx.Squad.Web.Services;

namespace Rivexx.Squad.Web.Pages;

public class IndexModel(
    ISquadOrchestrator orchestrator,
    IRivexxService rivexxService,
    ILlmClient llmClient) : PageModel
{
    public const string DefaultBriefing = """
        A Rivexx Componentes possui duas plantas e tres turnos. A empresa precisa de uma aplicacao web interna
        para registrar nao conformidades com evidencias auditaveis, assistir a analise de causa raiz e monitorar
        acoes corretivas, alem de rastrear lotes da materia-prima ao produto expedido. A interface deve ser responsiva
        e operavel sem treinamento tecnico. O primeiro caso e um defeito dimensional na linha 4.
        """;

    public SquadRun? Run { get; private set; }
    public DashboardMetrics Metrics { get; private set; } = new(0, 0, 0, 0);
    public bool LlmConfigured => llmClient.IsConfigured;
    public string LlmModel => llmClient.Model;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Run = await orchestrator.GetLatestAsync(cancellationToken);
        Metrics = await rivexxService.GetMetricsAsync(cancellationToken);
    }
}
