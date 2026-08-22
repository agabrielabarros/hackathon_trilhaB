using Microsoft.AspNetCore.Mvc.RazorPages;
using Rivexx.Squad.Web.Domain;
using Rivexx.Squad.Web.Services;

namespace Rivexx.Squad.Web.Pages;

public sealed class CausaRaizModel(IRivexxService service) : PageModel
{
    public IReadOnlyList<Nonconformity> Records { get; private set; } = [];
    public IReadOnlyList<RootCauseView> Analyses { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Records = await service.GetNonconformitiesAsync(cancellationToken);
        Analyses = await service.GetAnalysesAsync(cancellationToken);
    }
}
