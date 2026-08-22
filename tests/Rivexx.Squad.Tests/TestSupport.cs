using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using Rivexx.Squad.Web.Domain;
using Rivexx.Squad.Web.Infrastructure;
using Rivexx.Squad.Web.Services;

namespace Rivexx.Squad.Tests;

internal sealed class FakeStore : IAppStore
{
    public AppState State { get; } = new()
    {
        ProductionLots =
        [
            new ProductionLot
            {
                LotCode = "RVX-2026-0042", Product = "Carcaca tecnica X42",
                MaterialLot = "MP-PA66-8821", Supplier = "Polimeros Sul",
                Equipment = "Injetora INJ-04", Shift = "2o turno",
                Operators = "Ana Lima, Carlos Nunes", RelatedLots = "RVX-2026-0041",
                Destination = "AutoParts Brasil", Status = "Quarentena"
            }
        ]
    };

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<T> ReadAsync<T>(Func<AppState, T> query, CancellationToken cancellationToken = default) =>
        Task.FromResult(query(State));

    public Task<T> MutateAsync<T>(Func<AppState, T> command, CancellationToken cancellationToken = default) =>
        Task.FromResult(command(State));
}

internal sealed class TestEnvironment(string contentRoot) : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "Rivexx.Squad.Tests";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = contentRoot;
    public string EnvironmentName { get; set; } = "Testing";
    public string ContentRootPath { get; set; } = contentRoot;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

internal sealed class TestTimeProvider : TimeProvider
{
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => FixedNow;
}

internal sealed class DisabledLlmClient : ILlmClient
{
    public bool IsConfigured => false;
    public string Model => "test-model";

    public Task<LlmCallResult<T>> GenerateAsync<T>(
        string schemaName,
        object schema,
        string instructions,
        string input,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(LlmCallResult<T>.Failure(Model, "LLM desabilitado no teste."));
}

internal sealed class TemporaryWorkspace : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), $"rivexx-{Guid.NewGuid():N}");
    public string WebRoot => Path.Combine(Root, "src", "Rivexx.Squad.Web");

    public TemporaryWorkspace() => Directory.CreateDirectory(WebRoot);

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, true);
        }
    }
}
