namespace Rivexx.Squad.Web.Services;

public sealed class LlmOptions
{
    public const string SectionName = "OpenAI";

    public bool Enabled { get; set; } = true;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-5.6-terra";
    public string Endpoint { get; set; } = "https://api.openai.com/v1/responses";
    public string ReasoningEffort { get; set; } = "low";
    public int TimeoutSeconds { get; set; } = 90;
    public int MaxOutputTokens { get; set; } = 6000;
}
