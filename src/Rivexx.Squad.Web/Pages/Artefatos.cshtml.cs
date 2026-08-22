using Microsoft.AspNetCore.Mvc.RazorPages;
using Rivexx.Squad.Web.Domain;
using Rivexx.Squad.Web.Services;

namespace Rivexx.Squad.Web.Pages;

public sealed class ArtefatosModel(ISquadOrchestrator orchestrator) : PageModel
{
    public SquadRun? Run { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        Run = await orchestrator.GetLatestAsync(cancellationToken);
}
