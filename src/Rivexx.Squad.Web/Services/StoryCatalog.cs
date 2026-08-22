namespace Rivexx.Squad.Web.Services;

internal sealed record StoryDefinition(
    string Key,
    string Title,
    int Priority,
    string Description,
    string[] AcceptanceCriteria,
    string Decision,
    string Rationale);

internal static class StoryCatalog
{
    public static readonly StoryDefinition[] All =
    [
        new(
            "US-01",
            "Registrar nao conformidade no celular",
            1,
            "Como operador, quero registrar um defeito dimensional da linha 4 para iniciar a investigacao com evidencias auditaveis.",
            [
                "Formulario responsivo e operavel sem treinamento",
                "Data, responsavel, turno, equipamento, lote e evidencia obrigatorios",
                "Registro persistido e confirmado ao operador"
            ],
            "Usar validacao no servidor e uma interface mobile-first.",
            "Mantem as regras auditaveis mesmo quando o cliente desabilita JavaScript."),
        new(
            "US-02",
            "Assistir a analise de causa raiz",
            2,
            "Como coordenador, quero receber causas provaveis e um plano corretivo para transformar historico em acao monitoravel.",
            [
                "Aplicar metodologia estruturada de 5 Porques",
                "Exibir causa sugerida, confianca e evidencia historica",
                "Gerar acao com responsavel, prazo e status"
            ],
            "Combinar regras explicaveis com historico de defeitos semelhantes.",
            "A banca consegue verificar a origem da sugestao e o nivel de confianca."),
        new(
            "US-03",
            "Rastrear lote de ponta a ponta",
            3,
            "Como coordenador, quero informar um lote e recuperar toda a cadeia para responder ao cliente em segundos.",
            [
                "Localizar o lote pelo codigo exato",
                "Mostrar materia-prima, fornecedor, equipamento, turno e operadores",
                "Mostrar lotes correlatos, destino e status"
            ],
            "Modelar a rastreabilidade como leitura indexada por codigo de lote.",
            "Uma consulta simples e reproduzivel atende ao tempo critico da operacao.")
    ];
}
