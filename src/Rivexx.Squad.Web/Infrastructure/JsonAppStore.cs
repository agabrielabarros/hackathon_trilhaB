using System.Text.Json;
using Rivexx.Squad.Web.Domain;

namespace Rivexx.Squad.Web.Infrastructure;

public interface IAppStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<T> ReadAsync<T>(Func<AppState, T> query, CancellationToken cancellationToken = default);
    Task<T> MutateAsync<T>(Func<AppState, T> command, CancellationToken cancellationToken = default);
}

public sealed class JsonAppStore(IWebHostEnvironment environment) : IAppStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly string _path = Path.Combine(environment.ContentRootPath, "App_Data", "app-state.json");
    private AppState _state = new();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            if (File.Exists(_path))
            {
                await using var stream = File.OpenRead(_path);
                _state = await JsonSerializer.DeserializeAsync<AppState>(stream, _jsonOptions, cancellationToken)
                    ?? CreateSeedState();
            }
            else
            {
                _state = CreateSeedState();
                await PersistAsync(cancellationToken);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<T> ReadAsync<T>(Func<AppState, T> query, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return query(_state);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<T> MutateAsync<T>(Func<AppState, T> command, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var result = command(_state);
            await PersistAsync(cancellationToken);
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        var temporaryPath = $"{_path}.tmp";
        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, _state, _jsonOptions, cancellationToken);
        }

        File.Move(temporaryPath, _path, true);
    }

    private static AppState CreateSeedState() => new()
    {
        ProductionLots =
        [
            new ProductionLot
            {
                LotCode = "RVX-2026-0042", Product = "Carcaca tecnica X42",
                MaterialLot = "MP-PA66-8821", Supplier = "Polimeros Sul",
                Equipment = "Injetora INJ-04", Shift = "2o turno",
                Operators = "Ana Lima, Carlos Nunes",
                RelatedLots = "RVX-2026-0041, RVX-2026-0043",
                Destination = "AutoParts Brasil - CD Campinas", Status = "Quarentena"
            },
            new ProductionLot
            {
                LotCode = "RVX-2026-0041", Product = "Carcaca tecnica X42",
                MaterialLot = "MP-PA66-8821", Supplier = "Polimeros Sul",
                Equipment = "Injetora INJ-04", Shift = "1o turno",
                Operators = "Joao Reis, Marina Alves", RelatedLots = "RVX-2026-0042",
                Destination = "Estoque Rivexx - Planta 1", Status = "Inspecao"
            },
            new ProductionLot
            {
                LotCode = "RVX-2026-0043", Product = "Carcaca tecnica X42",
                MaterialLot = "MP-PA66-8821", Supplier = "Polimeros Sul",
                Equipment = "Injetora INJ-04", Shift = "3o turno",
                Operators = "Lucas Prado, Beatriz Melo", RelatedLots = "RVX-2026-0042",
                Destination = "EletroTech - CD Sorocaba", Status = "Expedido"
            }
        ]
    };
}
