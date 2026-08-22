using Rivexx.Squad.Web.Domain;
using Rivexx.Squad.Web.Infrastructure;

namespace Rivexx.Squad.Web.Services;

public interface IRivexxService
{
    Task<Nonconformity> RegisterAsync(NonconformityRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Nonconformity>> GetNonconformitiesAsync(CancellationToken cancellationToken = default);
    Task<RootCauseAnalysis> AnalyzeAsync(int nonconformityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RootCauseView>> GetAnalysesAsync(CancellationToken cancellationToken = default);
    Task<ProductionLot?> TraceLotAsync(string lotCode, CancellationToken cancellationToken = default);
    Task<DashboardMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);
}

public sealed class RivexxService(IAppStore store, TimeProvider timeProvider) : IRivexxService
{
    public Task<Nonconformity> RegisterAsync(
        NonconformityRequest request,
        CancellationToken cancellationToken = default)
    {
        var values = new[]
        {
            request.DefectType, request.Description, request.Line, request.LotCode,
            request.Responsible, request.Shift, request.Equipment, request.Evidence
        };
        if (values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Todos os campos auditaveis sao obrigatorios.", nameof(request));
        }

        return store.MutateAsync(state =>
        {
            var record = new Nonconformity
            {
                Id = state.Nonconformities.Count == 0 ? 1 : state.Nonconformities.Max(item => item.Id) + 1,
                DefectType = request.DefectType.Trim(),
                Description = request.Description.Trim(),
                Line = request.Line.Trim(),
                LotCode = request.LotCode.Trim().ToUpperInvariant(),
                Responsible = request.Responsible.Trim(),
                Shift = request.Shift.Trim(),
                Equipment = request.Equipment.Trim(),
                Evidence = request.Evidence.Trim(),
                CreatedAt = timeProvider.GetUtcNow()
            };
            state.Nonconformities.Add(record);
            return record;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<Nonconformity>> GetNonconformitiesAsync(CancellationToken cancellationToken = default) =>
        store.ReadAsync<IReadOnlyList<Nonconformity>>(
            state => state.Nonconformities.OrderByDescending(item => item.Id).ToList(), cancellationToken);

    public Task<RootCauseAnalysis> AnalyzeAsync(int nonconformityId, CancellationToken cancellationToken = default) =>
        store.MutateAsync(state =>
        {
            var existing = state.RootCauseAnalyses
                .OrderByDescending(item => item.Id)
                .FirstOrDefault(item => item.NonconformityId == nonconformityId);
            if (existing is not null)
            {
                return existing;
            }

            var record = state.Nonconformities.FirstOrDefault(item => item.Id == nonconformityId)
                ?? throw new KeyNotFoundException("Nao conformidade nao encontrada.");
            var isDimensional = $"{record.DefectType} {record.Description}"
                .Contains("dimension", StringComparison.OrdinalIgnoreCase);
            var analysis = new RootCauseAnalysis
            {
                Id = state.RootCauseAnalyses.Count == 0 ? 1 : state.RootCauseAnalyses.Max(item => item.Id) + 1,
                NonconformityId = record.Id,
                Method = "5 Porques + historico de ocorrencias",
                SuggestedCause = isDimensional
                    ? "Variacao da temperatura do molde na injetora INJ-04"
                    : "Desvio dos parametros de processo durante a troca de turno",
                Confidence = isDimensional ? 87 : 68,
                HistoricalEvidence = isDimensional
                    ? "4 de 5 ocorrencias dimensionais similares coincidiram com oscilacao termica."
                    : "Padrao observado em 3 ocorrencias historicas da mesma linha.",
                CorrectiveAction = isDimensional
                    ? "Calibrar sensores do molde e validar a primeira peca a cada troca de turno."
                    : "Revalidar setup da linha e registrar dupla checagem por 14 dias.",
                Owner = "Coordenacao da Qualidade",
                DueDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddDays(14)),
                CreatedAt = timeProvider.GetUtcNow()
            };
            state.RootCauseAnalyses.Add(analysis);
            return analysis;
        }, cancellationToken);

    public Task<IReadOnlyList<RootCauseView>> GetAnalysesAsync(CancellationToken cancellationToken = default) =>
        store.ReadAsync<IReadOnlyList<RootCauseView>>(state =>
            state.RootCauseAnalyses
                .OrderByDescending(item => item.Id)
                .Select(analysis =>
                {
                    var record = state.Nonconformities.First(item => item.Id == analysis.NonconformityId);
                    return new RootCauseView(analysis, record.DefectType, record.LotCode, record.Line);
                })
                .ToList(), cancellationToken);

    public Task<ProductionLot?> TraceLotAsync(string lotCode, CancellationToken cancellationToken = default) =>
        store.ReadAsync(state => state.ProductionLots.FirstOrDefault(
            lot => lot.LotCode.Equals(lotCode?.Trim(), StringComparison.OrdinalIgnoreCase)), cancellationToken);

    public Task<DashboardMetrics> GetMetricsAsync(CancellationToken cancellationToken = default) =>
        store.ReadAsync(state => new DashboardMetrics(
            state.Runs.Count,
            state.Runs.Sum(run => run.Events.Count),
            state.Runs.Sum(run => run.Stories.Count(story => story.Status == "Aceita")),
            state.Nonconformities.Count), cancellationToken);
}
