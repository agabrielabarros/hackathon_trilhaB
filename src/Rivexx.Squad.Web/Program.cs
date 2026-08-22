using Rivexx.Squad.Web.Domain;
using Rivexx.Squad.Web.Infrastructure;
using Rivexx.Squad.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));
builder.Services.PostConfigure<LlmOptions>(options =>
{
    var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (!string.IsNullOrWhiteSpace(apiKey)) options.ApiKey = apiKey;

    var model = Environment.GetEnvironmentVariable("OPENAI_MODEL");
    if (!string.IsNullOrWhiteSpace(model)) options.Model = model;
});
builder.Services.AddHttpClient("OpenAI");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAppStore, JsonAppStore>();
builder.Services.AddSingleton<ILlmClient, OpenAiResponsesClient>();
builder.Services.AddSingleton<IAgentTeam, AgentTeam>();
builder.Services.AddSingleton<ISquadOrchestrator, SquadOrchestrator>();
builder.Services.AddSingleton<IRivexxService, RivexxService>();

var app = builder.Build();
await app.Services.GetRequiredService<IAppStore>().InitializeAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/llm/status", (ILlmClient llm) => Results.Ok(new
{
    configured = llm.IsConfigured,
    model = llm.Model,
    fallback = "offline"
}));
app.MapPost("/api/squad/run", async (
    BriefingRequest request,
    ISquadOrchestrator orchestrator,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await orchestrator.RunAsync(request.Briefing, cancellationToken));
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["briefing"] = [exception.Message]
        });
    }
});
app.MapPost("/api/nonconformities", async (
    NonconformityRequest request,
    IRivexxService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Created($"/api/nonconformities", await service.RegisterAsync(request, cancellationToken));
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["record"] = [exception.Message]
        });
    }
});
app.MapPost("/api/nonconformities/{id:int}/root-cause", async (
    int id,
    IRivexxService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Created($"/api/nonconformities/{id}/root-cause", await service.AnalyzeAsync(id, cancellationToken));
    }
    catch (KeyNotFoundException exception)
    {
        return Results.NotFound(new { detail = exception.Message });
    }
});
app.MapGet("/api/lots/{lotCode}", async (
    string lotCode,
    IRivexxService service,
    CancellationToken cancellationToken) =>
{
    var lot = await service.TraceLotAsync(lotCode, cancellationToken);
    return lot is null ? Results.NotFound(new { detail = "Lote nao encontrado." }) : Results.Ok(lot);
});

app.Run();

public partial class Program;
