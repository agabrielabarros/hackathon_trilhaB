# Rivexx Autonomous Squad - C#

MVP da Trilha B do Hackathon Reply 2026: um squad autonomo de PO, Dev e QA que
recebe o briefing da Rivexx, cria o backlog, gera codigo C#, executa quality gates e
entrega uma aplicacao web auditavel para nao conformidades industriais.

## Funcionalidades

- Fluxo PO -> Dev -> QA depois de um unico briefing.
- Tres chamadas independentes ao LLM pela OpenAI Responses API, com saidas estruturadas por JSON Schema.
- Fallback automatico por agente para a execucao local quando a API estiver indisponivel ou devolver uma saida invalida.
- Comunicacao explicita em uma linha do tempo auditavel.
- Backlog priorizado com criterios de aceite.
- Codigo C# e decisoes produzidos pelo Dev Agent.
- Casos executados e liberacao registrada pelo QA Agent.
- Registro responsivo de defeito dimensional da linha 4.
- Causa raiz assistida com evidencia historica e plano corretivo.
- Rastreabilidade do lote `RVX-2026-0042` de ponta a ponta.
- Artefatos persistidos em `generated/runs/<id>`.

## Requisitos

- .NET SDK 10.0 ou superior.

## Executar

Defina a chave somente no ambiente do terminal (ela nao deve entrar no `appsettings.json`):

```powershell
$env:OPENAI_API_KEY="sua-chave"
$env:OPENAI_MODEL="gpt-5.6-terra" # opcional
```

Sem `OPENAI_API_KEY`, a aplicacao continua funcional em modo offline e informa esse modo no painel.

```powershell
dotnet restore Hackathon.slnx
dotnet run --project src/Rivexx.Squad.Web --urls http://127.0.0.1:5080
```

Acesse [http://127.0.0.1:5080](http://127.0.0.1:5080).

## Compilar e testar

```powershell
dotnet build Hackathon.slnx
dotnet test Hackathon.slnx --no-build
```

## Roteiro da demo

1. Abra **Squad em acao** e dispare o briefing pre-preenchido.
2. Mostre as 12 mensagens do fluxo PO -> Dev -> QA.
3. Em **Registro agil**, grave o defeito dimensional da linha 4.
4. Em **Causa raiz**, gere a analise e mostre confianca, evidencia e acao.
5. Em **Rastreabilidade**, consulte `RVX-2026-0042`.
6. Termine em **Artefatos**, exibindo backlog, decisoes e relatorio de QA.

## Estrutura

```text
src/Rivexx.Squad.Web/
  Domain/          # contratos de dominio e API
  Infrastructure/  # persistencia JSON local
  Services/        # orquestrador, agentes e regras Rivexx
  Pages/           # Razor Pages da demonstracao
  wwwroot/         # interface responsiva local
tests/Rivexx.Squad.Tests/
docs/
generated/
```

As decisoes tecnicas estao em [docs/architecture.md](docs/architecture.md).

O status da integracao pode ser consultado em `GET /api/llm/status`; a resposta nunca inclui a chave.
