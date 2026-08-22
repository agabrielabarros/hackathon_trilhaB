using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rivexx.Squad.Web.Domain;
using Rivexx.Squad.Web.Services;

namespace Rivexx.Squad.Web.Pages;

public sealed class RastreabilidadeModel(IRivexxService service) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "lote")]
    public string Lote { get; set; } = string.Empty;

    public ProductionLot? Lot { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(Lote))
        {
            Lot = await service.TraceLotAsync(Lote, cancellationToken);
        }
    }
}
