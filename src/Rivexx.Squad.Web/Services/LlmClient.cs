using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Rivexx.Squad.Web.Services;

public sealed record LlmCallResult<T>(
    bool Succeeded,
    T? Value,
    string Model,
    string? ResponseId,
    string? Error)
{
    public static LlmCallResult<T> Failure(string model, string error) =>
        new(false, default, model, null, error);
}

public interface ILlmClient
{
    bool IsConfigured { get; }
    string Model { get; }
    Task<LlmCallResult<T>> GenerateAsync<T>(
        string schemaName,
        object schema,
        string instructions,
        string input,
        CancellationToken cancellationToken = default);
}

public sealed class OpenAiResponsesClient(
    IHttpClientFactory httpClientFactory,
    IOptions<LlmOptions> options,
    ILogger<OpenAiResponsesClient> logger) : ILlmClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly LlmOptions _options = options.Value;

    public bool IsConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);
    public string Model => _options.Model;

    public async Task<LlmCallResult<T>> GenerateAsync<T>(
        string schemaName,
        object schema,
        string instructions,
        string input,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return LlmCallResult<T>.Failure(Model, "OPENAI_API_KEY nao configurada; fallback offline ativado.");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Content = JsonContent.Create(new
            {
                model = _options.Model,
                instructions,
                input,
                store = false,
                max_output_tokens = _options.MaxOutputTokens,
                reasoning = new { effort = _options.ReasoningEffort },
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = schemaName,
                        strict = true,
                        schema
                    }
                }
            }, options: JsonOptions);

            var client = httpClientFactory.CreateClient("OpenAI");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                var message = ExtractApiError(body) ?? $"OpenAI retornou HTTP {(int)response.StatusCode}.";
                logger.LogWarning("OpenAI request failed: {Status} {Message}", response.StatusCode, message);
                return LlmCallResult<T>.Failure(Model, message);
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var responseId = root.TryGetProperty("id", out var id) ? id.GetString() : null;
            var outputText = ExtractOutputText(root);
            if (string.IsNullOrWhiteSpace(outputText))
            {
                return LlmCallResult<T>.Failure(Model, "A resposta nao continha output_text estruturado.");
            }

            var value = JsonSerializer.Deserialize<T>(outputText, JsonOptions);
            return value is null
                ? LlmCallResult<T>.Failure(Model, "Nao foi possivel desserializar a resposta estruturada.")
                : new LlmCallResult<T>(true, value, Model, responseId, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return LlmCallResult<T>.Failure(Model, $"Timeout de {_options.TimeoutSeconds}s ao consultar o LLM.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            logger.LogWarning(exception, "OpenAI call failed; offline fallback will be used.");
            return LlmCallResult<T>.Failure(Model, exception.Message);
        }
    }

    private static string? ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var direct) && direct.ValueKind == JsonValueKind.String)
        {
            return direct.GetString();
        }

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var type)
                    && type.GetString() == "output_text"
                    && part.TryGetProperty("text", out var text))
                {
                    return text.GetString();
                }

                if (part.TryGetProperty("type", out type) && type.GetString() == "refusal"
                    && part.TryGetProperty("refusal", out var refusal))
                {
                    return null;
                }
            }
        }

        return null;
    }

    private static string? ExtractApiError(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message)
                    ? message.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
