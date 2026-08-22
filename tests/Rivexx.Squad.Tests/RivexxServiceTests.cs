using Rivexx.Squad.Web.Domain;
using Rivexx.Squad.Web.Services;

namespace Rivexx.Squad.Tests;

public sealed class RivexxServiceTests
{
    [Fact]
    public async Task DimensionalDefectProducesExplainableRootCause()
    {
        var service = new RivexxService(new FakeStore(), new TestTimeProvider());
        var record = await service.RegisterAsync(new NonconformityRequest(
            "Defeito dimensional", "Largura 0,8 mm acima da tolerancia", "Linha 4",
            "RVX-2026-0042", "Ana Lima", "2o turno", "Injetora INJ-04", "Medicao PAC-042"));

        var analysis = await service.AnalyzeAsync(record.Id);

        Assert.Equal(87, analysis.Confidence);
        Assert.Contains("temperatura", analysis.SuggestedCause, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new DateOnly(2026, 9, 5), analysis.DueDate);
    }

    [Fact]
    public async Task SeededLotReturnsTheCompleteChain()
    {
        var service = new RivexxService(new FakeStore(), new TestTimeProvider());

        var lot = await service.TraceLotAsync("rvx-2026-0042");

        Assert.NotNull(lot);
        Assert.Equal("MP-PA66-8821", lot.MaterialLot);
        Assert.Equal("Polimeros Sul", lot.Supplier);
        Assert.NotEmpty(lot.Operators);
        Assert.NotEmpty(lot.Destination);
    }

    [Fact]
    public async Task RegistrationRequiresEveryAuditableField()
    {
        var service = new RivexxService(new FakeStore(), new TestTimeProvider());
        var request = new NonconformityRequest(
            "Defeito dimensional", "", "Linha 4", "RVX-2026-0042",
            "Ana Lima", "2o turno", "Injetora INJ-04", "Medicao PAC-042");

        await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(request));
    }
}
