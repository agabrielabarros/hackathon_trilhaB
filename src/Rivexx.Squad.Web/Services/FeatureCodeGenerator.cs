namespace Rivexx.Squad.Web.Services;

internal static class FeatureCodeGenerator
{
    public static string Generate(string storyKey) => storyKey switch
    {
        "US-01" => """
            namespace Rivexx.Generated;

            public static class NonconformityRegistration
            {
                private static readonly string[] RequiredFields =
                [
                    "defectType", "description", "line", "lotCode",
                    "responsible", "shift", "equipment", "evidence"
                ];

                public static string[] Validate(IReadOnlyDictionary<string, string> payload) =>
                    RequiredFields.Where(field => !payload.TryGetValue(field, out var value)
                        || string.IsNullOrWhiteSpace(value)).ToArray();
            }
            """,
        "US-02" => """
            namespace Rivexx.Generated;

            public static class AssistedRootCause
            {
                public static (string Cause, int Confidence) Suggest(string defectType) =>
                    defectType.Contains("dimension", StringComparison.OrdinalIgnoreCase)
                        ? ("Variacao de temperatura do molde", 87)
                        : ("Revisar parametros de processo e materia-prima", 62);
            }
            """,
        "US-03" => """
            namespace Rivexx.Generated;

            public static class LotTraceability
            {
                public static object BuildTrace(dynamic lot) => new
                {
                    lot.MaterialLot, lot.Supplier, lot.Equipment, lot.Shift,
                    lot.Operators, lot.RelatedLots, lot.Destination, lot.Status
                };
            }
            """,
        _ => throw new ArgumentOutOfRangeException(nameof(storyKey), storyKey, "Story sem gerador")
    };
}
