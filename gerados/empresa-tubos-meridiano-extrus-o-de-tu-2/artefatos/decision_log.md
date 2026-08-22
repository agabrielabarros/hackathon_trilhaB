# Log de decisões técnicas — Controle de Perda de Resina

`run empresa-tubos-meridiano-extrus-o-de-tu-2-86146f` · **Dev Agent**


## Lançamento de Turno — `lancamento-turno.html`

Construir um formulário responsivo e offline-first com validação em tempo real, localStorage para draft automático e sincronização assíncrona. A interface será simples e visual (buttons para linha, dropdowns para turno), com campos numéricos restritos e desabilitação inteligente do botão Enviar. Implementar retry automático de envios falhos com feedback claro ao usuário.

*Arquivo gerado: 28506 caracteres · retrabalhos: 2*


### Usar localStorage + IndexedDB (fallback) para armazenar drafts antes do envio

- **Justificativa:** ST-05: supervisor trabalha offline no chão de fábrica. localStorage é suficiente para um draft por turno; IndexedDB garante persistência mesmo após limpeza de cache.
- **Alternativas descartadas:** Session storage (perde ao fechar aba); apenas estado em memória (perde ao desconectar da rede)
- **Impacto:** Dados|Performance|Arquitetura

### Pré-preencher data como hoje (new Date()) e responsável como usuário autenticado (read-only)

- **Justificativa:** ST-01 + Restrição cliente: guardar data, turno e responsável automaticamente. Reduz carga de entrada e evita erros de digitação.
- **Alternativas descartadas:** Permitir edição de data (viola critério de automação); deixar responsável em branco (perde rastreabilidade)
- **Impacto:** UX|Auditoria

### Linha: usar botões grandes e clicáveis (não dropdown) para celular touch-friendly; turno: dropdown ou toggle buttons

- **Justificativa:** ST-01 + Restrição: sem treinamento e acesso por celular. Botões de linha são mais óbvios (visual forte) que dropdown; turno são só 2 opções, toggle ou dropdown ambos ok.
- **Alternativas descartadas:** Dropdown para linha (pior em mobile, menos óbvio para papel-e-caneta); selector nativo HTML (não customizável)
- **Impacto:** UX|Acessibilidade

### Validação client-side em tempo real (onChange) com disable do botão Enviar até ALL obrigatórios preenchidos

- **Justificativa:** ST-01: 'botão Enviar fica desabilitado até todos os campos obrigatórios serem preenchidos'. Feedback imediato reduz cliques inúteis em chão de fábrica com conexão instável.
- **Alternativas descartadas:** Validação apenas ao submeter (ruim UX); validação server-only (sem offline)
- **Impacto:** UX|Performance

### Campos numéricos: usar input type='number' com min=0, step=0.1 (max 1 casa decimal) + validação regex no onChange

- **Justificativa:** ST-01: 'campos numéricos aceitam apenas números positivos com até 1 casa decimal'. Input nativo + validação garante consistência e teclado numérico em mobile.
- **Alternativas descartadas:** type='text' + máscara (complexo); input type='decimal' (não suportado universalmente)
- **Impacto:** UX|Acessibilidade

### Implementar fila de sincronização em memória + localStorage com retry automático a cada 10s quando conexão volta

- **Justificativa:** ST-05: 'quando a conexão volta, envia automaticamente'. Usar visibilitychange + online/offline events para detectar reconexão. Retry exponencial (10s, 20s, 30s) evita spam.
- **Alternativas descartadas:** Polling constante (drena bateria); service worker apenas (complexidade em supervisor sem treinamento); webhook server-push (requer backend sempre disponível)
- **Impacto:** Arquitetura|Performance|Dados

### Toast ou banner inline (não modal) para mensagens de sincronização ('Salvo localmente', 'Sincronizado com sucesso')

- **Justificativa:** ST-05: feedback visual claro. Toast não bloqueia fluxo, óbvio em celular, segue pattern mobile moderno.
- **Alternativas descartadas:** Modal (bloqueia); console.log (invisível em produção)
- **Impacto:** UX

### Gerar UUID (client-side) para cada draft e usar idempotency key no POST para evitar duplicação

- **Justificativa:** ST-05: 'não podem ser duplicados se usuário clicar várias vezes'. Server recusa POST com mesmo idempotency-key dentro de 24h.
- **Alternativas descartadas:** Apenas disable botão (user pode forçar refresh); timestamp como ID (colisões em ms)
- **Impacto:** Dados|Auditoria

### Redirecionar para tela de resultado (slug resultado-turno) após sucesso de envio (ou após validação local se offline)

- **Justificativa:** ST-01: 'após clicar Enviar, a página redireciona para tela de resultado'. Se offline, redirecionar após salvar no localStorage.
- **Alternativas descartadas:** Ficar na mesma página (confunde usuário); aguardar sincronização antes de redirecionar (bloqueia em conexão lenta)
- **Impacto:** UX

### Incluir campo de 'motivo da parada' como textarea obrigatório apenas se 'horas paradas' > 0

- **Justificativa:** ST-01 + lógica de negócio: parada sem motivo é inútil. Validação condicional melhora dados.
- **Alternativas descartadas:** Sempre obrigatório (usuário preenche 'n/a' se não parou); sempre opcional (perde rastreabilidade)
- **Impacto:** Dados|UX

### Estrutura de dados (JSON) no localStorage: {id, linha, turno, data, responsavel, resina_consumida, tubo_bom, refugo_partida, purga_troca, sobra_corte, preco_resina, horas_paradas, motivo_parada, timestamp_criacao, timestamp_tentativa_sync, status}

- **Justificativa:** ST-05 + auditoria: timestamp de criação vs tentativa permite debug; status permite rastrear 'draft|syncing|synced|failed'.
- **Alternativas descartadas:** Flat object (sem status fica confuso); múltiplos localStorage keys (fragmentação)
- **Impacto:** Arquitetura|Auditoria|Dados

## Resultado da Perda — `resultado-perda.html`

Construir uma tela de resultado em dois painéis: (1) Resumo da operação com cards de métricas-chave (perda total, %, custo, status de meta) com semáforo visual; (2) Análise de composição com gráfico de pizza e tabela de detalhamento. Todos os cálculos ocorrem em memória (JavaScript) sem requisição ao backend na visualização; o registro grava apenas ao clicar 'Confirmar'. Usar toast/banner para avisos de diferença não explicada.

*Arquivo gerado: 24793 caracteres · retrabalhos: 2*


### Renderizar cálculos em tempo real com estado local (React useState ou similar)

- **Justificativa:** ST-02 exige 'sem recarregar a página' e 'resultado na hora'. Cálculos no frontend são imediatos e garantem UX responsiva. Dados vêm de campos já preenchidos (linha, turno, resina, tubo bom, refugo, purga, sobra).
- **Alternativas descartadas:** Debounce + requisição ao backend violaria latência; SSR/template renderizado não permite reatividade esperada.
- **Impacto:** Performance|UX

### Semáforo (Verde/Amarelo/Vermelho) como elemento visual principal do status de meta

- **Justificativa:** ST-02 critério de aceite define 3 faixas (OK <4%, Atenção 4-7%, Crítico >7%). Cores padronizadas de alerta reduzem carga cognitiva para supervisor em piso de fábrica. Sem treinamento, semáforo é universal.
- **Alternativas descartadas:** Apenas texto ou ícones minimalistas poderiam ser ignorados em ambiente de ruído fabril.
- **Impacto:** UX|Acessibilidade

### Gráfico de pizza (ou anel de rosca) mostrando composição percentual de refugo, purga, sobra e diferença não explicada

- **Justificativa:** ST-06 exige visualização clara da composição sem treinamento. Pizza é intuitiva para distribuição de partes. Diferença não explicada como fatia destacada reforça o aviso textual.
- **Alternativas descartadas:** Gráfico de barras horizontal ocuparia mais espaço em tela pequena (mobile/tablet em chão de fábrica); treemap seria cognitivamente pesado.
- **Impacto:** UX|Acessibilidade

### Campo de observação livre (textarea, 200 caracteres) condicional ao confirmar, com contador de caracteres

- **Justificativa:** ST-06 critério de aceite: supervisor explica diferenças. Textarea inline no formulário de confirmação evita modal extra. Contador previne submissão inválida.
- **Alternativas descartadas:** Campo obrigatório violaria UX (supervisor pode não ter observação); modal separado adiciona cliques.
- **Impacto:** UX|Auditoria

### Tabela de resumo (linha, turno, resina, tubo bom, horas paradas) antes dos cálculos para validação visual

- **Justificativa:** ST-02 exige 'resumo dos dados lançados para validação visual'. Colocar após cálculos reforça que supervisor pode revisar inputs antes de confirmar.
- **Alternativas descartadas:** Colocar em abas/modal espalharia fluxo visual e aumentaria cliques.
- **Impacto:** UX|Auditoria

### Aviso de diferença não explicada como banner sticky ou toast se diferença > 5% da perda total

- **Justificativa:** ST-06 critério de aceite específico. Sticky top ou toast não obstrui leitura de resultados, mas chama atenção imediata. Desaparece se supervisor confirma com observação ou ajusta dados.
- **Alternativas descartadas:** Modal bloqueante interromperia fluxo; apenas cor no gráfico seria insuficiente.
- **Impacto:** UX

### Botão 'Confirmar Registro' desabilitado até todos os campos obrigatórios serem preenchidos; ativa gravação em registros_turno com timestamp serverside

- **Justificativa:** ST-02: 'Confirmar Registro que grava tudo em registros_turno'. Timestamp gerado no servidor garante sincronismo e auditoria correta, não depende do relógio do dispositivo.
- **Alternativas descartadas:** Timestamp no cliente é impreciso e auditavelmente fraco.
- **Impacto:** Auditoria|Dados

### Exibir todas as fórmulas de cálculo em rodapé ou tooltip (perda total = (refugo + purga + sobra) - tubo bom; perda % = (perda total / resina consumida) * 100; custo = perda total * preço resina)

- **Justificativa:** Supervisor sem treinamento precisa entender como números foram gerados. Transparência reduz desconfiança. Restrição 'sem treinamento' exige clareza de lógica.
- **Alternativas descartadas:** Ocultar fórmulas aumenta risco de rejeição do resultado.
- **Impacto:** UX|Acessibilidade

### Usar vocabulário exato do cliente (linha de extrusão, turno, resina consumida, tubo bom, refugo de partida, purga de troca, sobra de corte, perda, perda percentual, horas paradas, preço da resina, custo da perda, meta de perda)

- **Justificativa:** PO forneceu lista explícita. Inconsistência terminológica confunde supervisor no piso e cria atritos com treinamento. Usar exatamente como escrito garante alinhamento.
- **Alternativas descartadas:** Simplificar ou padronizar interna levaria a retrabalho e rejeição de UX.
- **Impacto:** UX|Acessibilidade

## Comparativo por Linha e Turno — `comparativo-linhas.html`

Construir um painel reativo com tabela pivotada (linha × turno × períodos) exibindo perda percentual e contagem de registros, com coloração condicional (vermelho >7%, amarelo 4-7%) e ordenação clientside. Implementar gráfico de tendência 30 dias para linha selecionada via dropdown, com filtros de período (presets + custom range) que atualizam ambas as visualizações sincronizadamente.

*Arquivo gerado: 26732 caracteres · retrabalhos: 2*


### Tabela pivotada em HTML nativa (não grid component) com dados agrupados no backend

- **Justificativa:** ST-03 exige colunas de períodos lado a lado (7d, 30d, mês atual). Pivot reduz requests; agregar no backend (SQL GROUP BY linha, turno, período) é mais eficiente que clientside. Compatível com ordenação e filtro.
- **Alternativas descartadas:** Grid com linhas dinâmicas seria verboso; componente UI pesado (ag-grid) desnecessário para 5 linhas × 3 turnos
- **Impacto:** Performance|Arquitetura

### Coloração condicional em CSS (classes .critico, .atencao) baseada em valor percentual, aplicada via JavaScript após renderização

- **Justificativa:** Critério ST-03 'destacar em vermelho/amarelo' é regra visual clara. Classes CSS mantêm separação de concerns; JS compara perda (campo na resposta) e aplica classe.
- **Alternativas descartadas:** Inline styles resultariam em código sujo; condicional no backend é possível mas duplicaria lógica
- **Impacto:** UX|Arquitetura

### Ordenação clientside via atributo data-sort nas células, com função JavaScript Toggle ASC/DESC

- **Justificativa:** ST-03 exige 'ordenável por perda percentual descendente por padrão'. Dataset é pequeno (5L × 3T = 15 linhas máx). Clientside reduz latência; padrão descendente aplicado no carregamento inicial.
- **Alternativas descartadas:** Ordenação serverside requer novo endpoint e reload visual; clientside é UX melhor para dataset pequeno
- **Impacto:** Performance|UX

### Gráfico de linha (Chart.js ou similar leve) mostrando tendência 30 dias da linha selecionada, com eixo Y = perda %, eixo X = dias

- **Justificativa:** ST-03 pede 'gráfico mostrando tendência'. Linha é mais legível que barras para evolução temporal; seleção de linha via dropdown gatilho re-fetch dos 30 últimos registros agrupados por dia.
- **Alternativas descartadas:** Barras horizontais funcionam mas não transmitem evolução temporal; heatmap seria overkill
- **Impacto:** UX|Performance

### Filtros em dropdowns: Linha (Todas | L1-L5), Período (7d | 30d | Mês atual | Custom range com datepicker)

- **Justificativa:** ST-03 exige 'filtrar por linha e período'. Dropdowns são scannable; 'Todas' permite view global. Custom range atende ad-hoc análises. Filtros gatilham re-fetch via query params.
- **Alternativas descartadas:** Filtros em modais ou abas aumentariam complexidade; toggles não comportam custom range
- **Impacto:** UX

### Colunas da tabela: Linha | Turno | Perda % (7d) + Registros | Perda % (30d) + Registros | Perda % (Mês) + Registros

- **Justificativa:** ST-03 especifica 'para cada célula exibe perda percentual média E número de registros'. Dupla coluna (% + contagem) em cada período facilita análise de robustez (% em amostra pequena é menos confiável).
- **Alternativas descartadas:** Tooltip com contagem: menos visível; sublinhas para contagem: aumenta altura visual desnecessariamente
- **Impacto:** UX

### Endpoint backend: GET /api/perda/comparativo?linha=L1&periodo=7d retorna [{linha, turno, perda_pct, num_registros, data_tendencia: [...]}]

- **Justificativa:** Agregar no backend (SQL GROUP BY) economiza banda e evita lógica duplicada. Campo tendencia_json traz 30 dias pré-agregado por dia para popular gráfico sem segundo request.
- **Alternativas descartadas:** Dois endpoints (um para tabela, outro para gráfico) resulta em 2 requests sempre; graphql seria overhead para caso tão simples
- **Impacto:** Arquitetura|Performance

### Armazenar valor de linha selecionada em localStorage para persistir após reload

- **Justificativa:** UX: usuário que volta ao painel vê mesma linha que estava analisando. Não é requisito mas melhora fluxo.
- **Alternativas descartadas:** URL param é melhor para shareability; mas localStorage é fallback aceitável
- **Impacto:** UX

### Validação de data custom range: data_inicio < data_fim e ambas nos últimos 365 dias

- **Justificativa:** Evitar requests inválidas; limitar a 1 ano economiza processamento backend. Feedback visual (erro inline) se violado.
- **Alternativas descartadas:** Datepicker com range constraints nativos (max/min) é suficiente; validação adicional é redundante
- **Impacto:** Auditoria

## Consulta de Registros — `consulta-auditoria.html`

Construir uma tela de consulta auditável com formulário de filtros simples (linha, turno, data-range, motivo), tabela paginada de registros históricos com 10 colunas, painel lateral de detalhes imutável ao clicar numa linha, e exportação CSV. Todos os dados vêm de registros_turno com rastreabilidade garantida (responsável + timestamp).

*Arquivo gerado: 26524 caracteres · retrabalhos: 2*


### Formulário de filtros em sticky-top ou inline acima da tabela, não em modal

- **Justificativa:** Supervisores e auditores precisam manter os filtros visíveis enquanto exploram resultados; critério de aceite exige formulário de busca acessível
- **Alternativas descartadas:** Modal de filtros (oculta contexto), abas separadas (fragmenta a tarefa)
- **Impacto:** UX

### Tabela com 10 colunas (Data, Linha, Turno, Resina Consumida, Tubo Bom, Perda Total, Perda %, Custo, Responsável, Motivo da Parada) renderizada com bibliotecaDataGrid ou custom com overflow horizontal em mobile

- **Justificativa:** Critério de aceite especifica exatamente estas 10 colunas; mobile precisa de scroll horizontal porque 10 colunas não cabem em viewport pequena
- **Alternativas descartadas:** Ocultar colunas em mobile (perde rastreabilidade de Responsável/Motivo), truncar texto sem hover (prejudica auditoria)
- **Impacto:** UX, Acessibilidade

### Paginação back-end (padrão: 20 registros/página) com offset/limit no filtro de registros_turno

- **Justificativa:** Critério exige 'máx 20 registros por página'; em auditoria com potencial volume alto, back-end deve limitar e contar total; reduz carga
- **Alternativas descartadas:** Paginação client-side (carrega tudo de uma vez — risco de timeout e memória)
- **Impacto:** Performance, Arquitetura

### Ao clicar numa linha, abrir painel de detalhes (drawer ou modal) com todos os campos do registro + timestamp de criação, sem permitir edição

- **Justificativa:** Critério exige painel imutável ('exibe imutavelmente'); drawer/modal iso o detalhe e permite fechar sem perder contexto da tabela
- **Alternativas descartadas:** Expansão inline (quebra fluxo visual, difícil para múltiplas linhas), nova página (perde contexto de filtros)
- **Impacto:** UX, Auditoria

### Botão Exportar CSV valida filtros aplicados, constrói query com mesmos parâmetros (sem paginação), e faz download direto

- **Justificativa:** Critério exige 'registros filtrados'; usuários de auditoria esperam CSV com contexto dos filtros atuais, não todos os registros
- **Alternativas descartadas:** Exportar sem filtros (não atende caso de uso), exportar com modal de confirmação (overhead desnecessário)
- **Impacto:** Arquitetura, UX

### Campos de data (inicial, final) usar input type='date' nativo com min/max para evitar ranges inválidos

- **Justificativa:** Supervisores precisam de picker rápido; validação client-side reduz erros; nativo é acessível
- **Alternativas descartadas:** Textbox com máscara (fragmental em mobile), date-range-picker library (overkill, adiciona dependência)
- **Impacto:** UX, Acessibilidade

### Exibir Responsável + Timestamp (data/hora) em destaque no painel de detalhes, com labels claros ('Lançado por' e 'Em')

- **Justificativa:** Restrição do cliente: 'Auditável: guardar e exibir responsável e timestamp'; painel imutável garante integridade para rastreabilidade
- **Alternativas descartadas:** Timestamp em rodapé ou escondido (reduz visibilidade em auditoria)
- **Impacto:** Auditoria

### Usar terminologia exata do cliente: 'linha de extrusão', 'tubo bom', 'perda percentual', 'custo da perda', 'motivo da parada' nos labels e placeholders

- **Justificativa:** PO forneceu vocabulário; consistência terminológica evita confusão entre supervisores/auditores e reduz tempo de treinamento
- **Alternativas descartadas:** Termos genéricos ('linha', 'descarte', 'custo') — cria fricção e risco de interpretação errada em auditoria
- **Impacto:** UX, Auditoria

### Filtro 'motivo da parada' como input text-livre (não dropdown), com autocomplete opcional baseado em valores históricos

- **Justificativa:** Critério aceita 'texto livre'; auditores podem buscar variações de motivos; autocomplete ajuda sem forçar seleção
- **Alternativas descartadas:** Dropdown obrigatório (reduz flexibilidade, risco de motivos não pré-cadastrados não aparecerem)
- **Impacto:** UX

### Skeleton loader na tabela durante fetch; desabilitar filtros enquanto busca em progresso para evitar requisições concurrent

- **Justificativa:** Feedback visual reduz percepção de lag; desabilitar evita race conditions e comportamento imprevisível
- **Alternativas descartadas:** Spinner genérico (menos informativo), permitir cliques múltiplos (risco de requisições duplicadas)
- **Impacto:** UX, Performance

### Mensagem de erro clara se busca retornar 0 registros; sugerir revisar filtros em vez de exibir tabela vazia sem contexto

- **Justificativa:** Auditores podem interpretar silêncio como bug; mensagem educativa acelera próxima tentativa
- **Alternativas descartadas:** Tabela vazia sem feedback (confunde usuário)
- **Impacto:** UX
