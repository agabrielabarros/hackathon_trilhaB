# Backlog — Controle de Perda de Resina

`run empresa-tubos-meridiano-extrus-o-de-tu-2-86146f` · **PO Agent** · cliente: Tubos Meridiano

> Aplicação web para o supervisor registrar dados de produção a cada turno e calcular em tempo real a perda de resina por linha, turno e motivo, criando visibilidade para reduzir desperdício.


**Vocabulário do cliente:** linha de extrusão, turno, resina consumida, tubo bom, refugo de partida, purga de troca, sobra de corte, perda, perda percentual, horas paradas, motivo da parada, preço da resina, custo da perda, meta de perda


**Regras de negócio extraídas do briefing**

- Perda total em kg = resina consumida - tubo bom produzido
- Perda percentual = (perda total / resina consumida) × 100
- Custo da perda em R$ = perda total × preço da resina
- Status: OK (perda < 4%), Atenção (4% ≤ perda ≤ 7%), Crítico (perda > 7%)
- Refugo total = refugo de partida + purga de troca + sobra de corte
- Diferença não explicada = perda total - refugo total

## Tela: Lançamento de Turno (`lancamento-turno`, formulario)

Registrar os dados da linha ao fim do turno e receber o cálculo de perda na hora


### ST-01 — Registrar dados da linha ao fim do turno

**Prioridade:** Alta

Como supervisor, quero preencher um formulário simples com os dados da minha linha e turno, para criar o registro que será calculado automaticamente.

**Critérios de aceite**

- [ ] O formulário contém campos para linha, turno (com data pre-preenchida como hoje), resina consumida, tubo bom, refugo de partida, purga de troca, sobra de corte, preço da resina, horas paradas e motivo da parada
- [ ] O campo de linha exibe as 5 linhas de extrusão existentes (dropdown ou buttons)
- [ ] O campo de turno oferece as 2 opções: turno 1, turno 2
- [ ] Todos os campos numéricos aceitam apenas números positivos com até 1 casa decimal
- [ ] O responsável é preenchido automaticamente (usuário logado) e exibido read-only
- [ ] O botão Enviar fica desabilitado até todos os campos obrigatórios serem preenchidos
- [ ] Após clicar Enviar, a página redireciona para a tela de resultado

**Restrições do briefing atendidas**

- Sem treinamento: interface deve ser óbvia para quem usa papel e caneta
- Acesso por celular: responsivo e touch-friendly
- Guardar data, turno e responsável: implementar automaticamente

### ST-05 — Funcionar offline e sincronizar quando houver internet

**Prioridade:** Média

Como supervisor no chão de fábrica com conexão instável, quero lançar dados mesmo sem internet e sincronizar assim que conectar, para nunca perder um registro.

**Critérios de aceite**

- [ ] A aplicação armazena draft de lançamento no localStorage antes de enviar
- [ ] Se o envio falhar (sem internet ou servidor indisponível), exibe mensagem clara: 'Registro salvo localmente. Será enviado assim que houver conexão.'
- [ ] Quando a conexão volta, a aplicação envia automaticamente todos os registros pendentes
- [ ] O usuário recebe confirmação visual (toast ou banner) quando a sincronização for bem-sucedida
- [ ] Registros sincronizados não podem ser duplicados se o usuário clicar várias vezes

**Restrições do briefing atendidas**

- Supervisor lança pelo celular no chão de fábrica: conexão pode ser intermitente

## Tela: Resultado da Perda (`resultado-perda`, calculadora)

Exibir em tempo real o cálculo de perda total, percentual, custo e status contra meta


### ST-02 — Calcular e exibir perda na hora

**Prioridade:** Alta

Como supervisor, quero ver o cálculo de perda total, percentual e em reais imediatamente após lançar os dados, para saber o impacto financeiro do turno.

**Critérios de aceite**

- [ ] O resultado exibe perda total em kg (com 2 casas decimais)
- [ ] O resultado exibe perda percentual (com 1 casa decimal e símbolo %)
- [ ] O resultado exibe custo da perda em R$ (com 2 casas decimais)
- [ ] O resultado mostra o status da meta: OK (verde, perda < 4%), Atenção (amarelo, 4% ≤ perda ≤ 7%), Crítico (vermelho, perda > 7%)
- [ ] A tela exibe um resumo dos dados lançados (linha, turno, resina, tubo bom, etc) para validação visual
- [ ] A tela exibe a composição da perda: refugo de partida, purga de troca, sobra de corte em kg e % do total
- [ ] A tela exibe a diferença não explicada em kg (perda total - refugo total)
- [ ] Todos os cálculos ocorrem sem recarregar a página
- [ ] Existe um botão Confirmar Registro que grava tudo em registros_turno com timestamp

**Restrições do briefing atendidas**

- Resultado na hora, não no dia seguinte
- Sem recarregar página

### ST-06 — Validar composição da perda para investigar diferenças

**Prioridade:** Média

Como supervisor, quero ver a composição da perda (refugo, purga, sobra) versus a perda total calculada, para identificar se há material não contabilizado.

**Critérios de aceite**

- [ ] O resultado exibe refugo total em kg (refugo de partida + purga de troca + sobra de corte)
- [ ] O resultado exibe diferença não explicada em kg: perda total - refugo total
- [ ] Se a diferença for > 5% da perda total, um aviso aparece: 'Diferença não explicada detectada. Revisar pesagem?'
- [ ] A tela exibe um pequeno gráfico em pizza ou barra mostrando composição: refugo de partida %, purga %, sobra %, diferença %
- [ ] O supervisor pode inserir um campo de observação livre ao confirmar (até 200 caracteres) para explicar diferenças

**Restrições do briefing atendidas**

- Sem treinamento: visualização clara e óbvia da composição

## Tela: Comparativo por Linha e Turno (`comparativo-linhas`, painel)

Visualizar perda acumulada por linha, turno e período para priorizar ações


### ST-03 — Visualizar perda por linha e turno ao longo do tempo

**Prioridade:** Alta

Como supervisor geral ou gerente, quero comparar a perda acumulada de cada linha e turno em um período, para identificar onde dói mais e priorizar ação.

**Critérios de aceite**

- [ ] O painel exibe uma tabela com colunas: Linha, Turno, Períodos (últimos 7 dias, últimos 30 dias, mês atual)
- [ ] Para cada célula (linha/turno/período), exibe perda percentual média e número de registros
- [ ] A tabela é ordenável por perda percentual (descendente por padrão)
- [ ] Linhas/turnos com perda > 7% (crítico) aparecem destacadas em vermelho
- [ ] Linhas/turnos com perda 4-7% (atenção) aparecem em amarelo
- [ ] Existe um gráfico de barras ou linha mostrando tendência de perda de uma linha específica ao longo dos últimos 30 dias
- [ ] O usuário pode filtrar por linha (dropdown com as 5 linhas ou 'Todas')
- [ ] O usuário pode filtrar por período (últimos 7 dias, 30 dias, mês atual, custom range)
- [ ] O painel carrega dados de registros_turno agrupados e média

**Restrições do briefing atendidas**

- Atacar primeiro onde dói mais: visualização deve destacar o crítico

## Tela: Consulta de Registros (`consulta-auditoria`, consulta)

Buscar e visualizar registros históricos com data, turno, responsável e motivos de parada


### ST-04 — Consultar registros históricos com rastreabilidade

**Prioridade:** Média

Como supervisor ou auditor, quero buscar registros passados por linha, turno, data e motivo de parada, para validar dados e rastrear decisões.

**Critérios de aceite**

- [ ] A tela exibe um formulário de busca com filtros: linha, turno, data inicial, data final, motivo da parada (texto livre)
- [ ] O resultado exibe uma tabela com colunas: Data, Linha, Turno, Resina Consumida, Tubo Bom, Perda Total, Perda %, Custo, Responsável, Motivo da Parada
- [ ] A tabela é paginada (máx 20 registros por página)
- [ ] Cada linha é clicável e abre um painel de detalhes com todos os campos do registro e timestamp de criação
- [ ] O painel de detalhes exibe imutavelmente: quem lançou (responsável), quando foi lançado (data/hora), qual foi o turno
- [ ] Existe um botão Exportar (CSV) que baixa os registros filtrados
- [ ] Todos os dados carregam de registros_turno

**Restrições do briefing atendidas**

- Auditável: guardar e exibir responsável e timestamp
