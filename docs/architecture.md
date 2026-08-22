# Arquitetura do MVP em C#

## Objetivo

Entregar uma demonstracao local, reproduzivel e auditavel do fluxo PO -> Dev -> QA,
junto com os tres cenarios operacionais obrigatorios da Rivexx.

## Stack

- .NET 10 e ASP.NET Core Razor Pages.
- Persistencia JSON local com gravacao atomica e controle de concorrencia.
- Minimal APIs para as interacoes da demonstracao.
- OpenAI Responses API via `HttpClient`, sem dependencia adicional de SDK.
- Structured Outputs com JSON Schema estrito para PO, Dev e QA.
- xUnit para a suite automatizada.
- HTML, CSS e JavaScript locais, sem dependencia visual de CDN.

## Componentes

- `Domain/Models.cs`: contratos persistidos e DTOs da API.
- `Infrastructure/JsonAppStore.cs`: armazenamento local e dados de demonstracao.
- `Services/SquadOrchestrator.cs`: passagem de contexto, quality gate e artefatos.
- `Services/AgentTeam.cs`: prompts, schemas, validacao semantica e fallback por agente.
- `Services/LlmClient.cs`: adaptador da Responses API, timeout e tratamento seguro de erros.
- `Services/FeatureCodeGenerator.cs`: codigo C# produzido pelo Dev Agent.
- `Services/RivexxService.cs`: registro, causa raiz e rastreabilidade.
- `Pages`: cinco superficies da demonstracao.
- `generated/runs/<id>`: backlog, codigo, decisoes, eventos e relatorio de QA.

## Decisoes conscientes

1. **Resiliencia por etapa.** O PO, Dev e QA usam o LLM quando configurado; uma falha ou
   saida semanticamente invalida ativa apenas o fallback local daquela etapa. A execucao
   registra o modo `OpenAI`, `Hibrido` ou `Offline` e seus avisos.
2. **Credencial fora do repositorio.** A chave vem de `OPENAI_API_KEY`; configuracoes nao
   secretas ficam em `appsettings.json` e o endpoint de status nao revela a credencial.
3. **Geracao controlada.** O Dev produz C# somente na pasta de artefatos e nao executa
   comandos arbitrarios do sistema operacional.
4. **Defesa em profundidade.** Structured Outputs limitam o formato, validacoes semanticas
   conferem as tres stories e hard gates locais verificam estrutura e contrato do C#.
5. **Quality gate visivel.** Cada story recebe casos do QA e gates locais com evidencia persistida.
6. **JSON no MVP.** Facilita portabilidade e inspecao durante a banca; a interface
   `IAppStore` permite substituir por SQL Server ou SQLite.
