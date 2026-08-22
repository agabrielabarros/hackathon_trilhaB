# Log de decisões técnicas — Rastreador de Não Conformidades

`run empresa-rivexx-componentes-ind-stria-d-4-a37eda` · **Dev Agent**


## Registro de Não Conformidade — `registro-nao-conformidade.html`

Construir um formulário responsivo de registro de não conformidade otimizado para celular, com captura automática de contexto (data/hora/usuário/turno) e suporte offline. A interface será minimalista, sem campos desnecessários, com dropdowns pré-carregados e validação client-side imediata. Implementaremos sincronização automática em background com IndexedDB para offline-first resilience.

*Arquivo gerado: 29326 caracteres · retrabalhos: 2*


### Pré-preenchimento automático de data, hora, operador logado e turno — sem campos manuais para estes dados

- **Justificativa:** ST-01 exige 'sistema registra automaticamente' e 'sem campo manual'. Operador no chão não deve digitar dados que já existem no contexto. Reduz tempo e erros.
- **Alternativas descartadas:** Permitir edição manual desses campos aumentaria complexidade da UI e risco de auditoria; não faz sentido operacional.
- **Impacto:** UX|Auditoria

### Usar IndexedDB (não localStorage) para dados offline com estrutura de fila de sincronização

- **Justificativa:** ST-05 exige volume potencial alto de registros offline + controle de duplicação. IndexedDB suporta transações, índices e queries — localStorage é apenas string/JSON. Necessário rastrear estado de sincronização (pendente/sincronizado/falha).
- **Alternativas descartadas:** localStorage é limitado (~5MB), sem transações; Service Workers com Cache API não persistem dados estruturados; banco remoto só funciona online.
- **Impacto:** Dados|Performance

### Implementar detecção de conectividade com listener de eventos online/offline + health check periódico

- **Justificativa:** ST-05 exige 'detecta automaticamente' perda e restauração de conexão. Eventos online/offline do navegador são rápidos mas podem ser enganados por proxy; health check (ping ou fetch leve) confirma acesso real à API.
- **Alternativas descartadas:** Só confiar em eventos do navegador falha com conectividade parcial (ex: proxy captivo); só health check consome bateria; combinação é mais confiável.
- **Impacto:** Performance|Arquitetura

### Sincronização em background com retry exponencial e notificação ao usuário

- **Justificativa:** ST-05 exige 'detecta automaticamente' e 'não há duplicação'. Retry exponencial evita overhead de requisições; idempotência no backend (chave única + timestamp) previne duplicação. Notificação mantém transparência.
- **Alternativas descartadas:** Retry imediato sobrecarregaria rede intermitente; sem retry falha na primeira tentativa; sem idempotência há risco de duplicação.
- **Impacto:** Arquitetura|Performance

### Dropdown de equipamentos e matéria-prima carregados no mount e cacheados em IndexedDB

- **Justificativa:** ST-01 exige 'dropdown com lista' e funcionalidade offline (ST-05). Dados catalográficos raramente mudam; cache local permite operação offline e reduz requisições. Invalidação periódica (ex: a cada 24h) mantém dados frescos.
- **Alternativas descartadas:** Buscar da API sempre: falha offline; hardcoded: não escala com mudanças na planta; fetch-on-demand: latência em celular 3G.
- **Impacto:** Performance|Dados

### Validação client-side imediata com feedback inline; validação server-side redundante

- **Justificativa:** ST-01 exige 'interface operável sem treinamento'. Feedback imediato reduz retrabalho; validação server é defesa contra bypass ou dados corrompidos. Operador sente controle e velocidade.
- **Alternativas descartadas:** Só server-side: latência, experiência ruim em 3G; só client-side: vulnerável a bypass.
- **Impacto:** UX|Arquitetura

### Lote aceita entrada manual OU scanning de código de barras/QR (via camera ou input)

- **Justificativa:** ST-01 'entrada de texto ou scanning'. Operador pode digitar (sem equipamento) ou escanear (mais rápido, mais preciso). HTML5 input type='text' + File API para câmera é cross-browser.
- **Alternativas descartadas:** Apenas scanning exclui cenários sem câmera; apenas manual é lento e propenso a erro.
- **Impacto:** UX

### Resposta de sucesso com ID único visível + reset automático do formulário

- **Justificativa:** ST-01 exige 'retorna ID único' e 'volta a estado vazio'. ID é auditável (ST-01 restrição) e rasträvel; reset prepara para próximo registro sem click extra.
- **Alternativas descartadas:** Modal de confirmação adiciona passo; redirecionamento quebra fluxo de registro em série.
- **Impacto:** UX|Auditoria

### Layout mobile-first com flexbox/grid, sem scroll horizontal, zoom mínimo 1x

- **Justificativa:** ST-01 exige 'campos visíveis sem zoom ou scroll horizontal' em celular. Mobile-first força priorização; viewport correto (width=device-width) impede zoom não intencional.
- **Alternativas descartadas:** Desktop-first adapta mal; layout fixo quebra em variações de celular.
- **Impacto:** UX|Acessibilidade

### Indicador visual de sincronização (spinner + contador ou barra de progresso) durante ST-05

- **Justificativa:** ST-05 exige 'usuário vê progresso visual'. Transparência reduz percepção de lentidão e constrói confiança em offline-first. Spinner é convenção universal.
- **Alternativas descartadas:** Sem indicador: usuário não sabe se sincronização está acontecendo; toast discreto é perdido em cenários high-stress.
- **Impacto:** UX

### Turno seletor com 3 opções fixas (1/2/3) ou lookup dinâmico conforme planta

- **Justificativa:** ST-01 'seletor com opções 1/2/3'. Se fixo, inclui diretamente; se dinâmico, carrega via API + cache local. Assumir 3 turnos por padrão industrial; configuração por planta fica em estágio futuro.
- **Alternativas descartadas:** Input text: operador digita errado; radio buttons: espaço em celular.
- **Impacto:** UX

## Investigação de Causa Raiz — `investigacao-causa-raiz.html`

Construir uma calculadora de investigação estruturada com seção imutável de contexto (não conformidade), seletor de metodologia, captura de hipóteses e evidências com gravação imediata, e validação condicional que libera narrativa final apenas quando causa raiz for confirmada. A auditabilidade será garantida por timestamps, responsáveis e imutabilidade de registros de evidência.

*Arquivo gerado: 31733 caracteres · retrabalhos: 2*


### Seção superior exibindo não conformidade em modo somente leitura (readonly fields ou display puro)

- **Justificativa:** Critério de aceite exige exibição clara do contexto (lote, defeito, equipamento, operador, turno) sem permitir edição, evitando que a investigação se desvincule do caso original
- **Alternativas descartadas:** Abas separadas ou modal: aumentariam clicks; campos editáveis aqui corromperiam auditabilidade
- **Impacto:** UX | Auditoria

### Dropdown 'Metodologia' com opções pré-definidas (5 Porquês, Fishbone, FMEA) obrigatório antes de prosseguir

- **Justificativa:** Restrição do cliente exige metodologia estruturada; dropdown garante padronização e reprodutibilidade; obrigatoriedade força disciplina
- **Alternativas descartadas:** Campos de texto livre ou seleção múltipla: permitiriam variarão não controlada; radio buttons: menos escalável se opções crescerem
- **Impacto:** Arquitetura | UX

### Campo 'Hipótese de Causa' como textarea com limite de caracteres visível e validação

- **Justificativa:** Deve capturar suspeita inicial de forma estruturada; limite evita registros excessivos e mantém clareza na auditoria
- **Alternativas descartadas:** Input simples: insuficiente para documentação; sem limite: texto descontrolado prejudica rastreamento
- **Impacto:** UX | Dados

### Seção 'Evidências' com formulário inline (data, tipo dropdown, descrição, responsável) + botão 'Adicionar Evidência' que grava imediatamente na coleção 'investigacoes'

- **Justificativa:** Critério de aceite exige gravação imediata e lista visível; isso garante que nenhuma evidência seja perdida e permite ajustes iterativos; cada registro recebe timestamp e responsável automaticamente
- **Alternativas descartadas:** Armazenar tudo em memória até salvar final: risco de perda de dados; modal separada: reduz fluxo visual
- **Impacto:** Dados | Performance | Auditoria

### Campo 'Tipo de Evidência' como dropdown controlado (inspeção visual, teste, entrevista, consulta registro)

- **Justificativa:** Restrição de auditabilidade exige categorização clara; dropdown padroniza e facilita filtros posterior em auditorias
- **Alternativas descartadas:** Campo texto: permitiria entradas inconsistentes; checkboxes múltiplos: uma evidência é um tipo único
- **Impacto:** Dados | Auditoria

### Lista de evidências exibida com cada registro imutável (readonly) após gravação, com timestamp, responsável, tipo e descrição visíveis

- **Justificativa:** Restrição de rastreabilidade de todas as alterações; imutabilidade após gravação garante integridade do histórico
- **Alternativas descartadas:** Permitir edição: comprometeria auditoria; não exibir lista: operador perderia visão do que já foi registrado
- **Impacto:** Auditoria | UX

### Campo 'Causa Raiz Confirmada' como dropdown (Sim / Não / Pendente), obrigatório antes de salvar

- **Justificativa:** Critério de aceite exige confirmação explícita e lógica condicional; valores pré-definidos evitam ambiguidade
- **Alternativas descartadas:** Toggle ou checkbox: insuficiente para estado 'Pendente'; campo texto: não é padronizado
- **Impacto:** UX | Dados

### Selecionar 'Sim' em 'Causa Raiz Confirmada' libera campo 'Narrativa Final' (textarea) e habilita botão 'Salvar Investigação'

- **Justificativa:** Critério de aceite exige liberação condicional de narrativa e persistência apenas se concluída; regra de negócio clara: investigação só termina com causa raiz confirmada
- **Alternativas descartadas:** Sempre exibir narrativa: criaria confusão sobre quando é obrigatória; salvar em qualquer estado: violaria regra de negócio
- **Impacto:** Arquitetura | UX

### Ao marcar 'Sim' e salvar, registrar automaticamente data/hora, analista logado e turno na coleção 'investigacoes'

- **Justificativa:** Critério de aceite e restrição de auditabilidade; contexto automático elimina erros manuais e garante rastreabilidade completa
- **Alternativas descartadas:** Solicitar data/hora manualmente: erro humano; não registrar turno: perde contexto operacional
- **Impacto:** Auditoria | Dados

### Botão 'Salvar Investigação' validado para exigir: metodologia selecionada, hipótese preenchida, ao menos 1 evidência, causa raiz confirmada = 'Sim', narrativa final preenchida

- **Justificativa:** Critério de aceite exige persistência como 'Concluída' apenas se causa raiz confirmada; validação rigorosa garante qualidade do registro e compliance
- **Alternativas descartadas:** Permitir save parcial: deixaria investigações incompletas na base; validação fraca: aceitaria registros insuficientes
- **Impacto:** Arquitetura | Dados | Auditoria

### Usar estrutura de dados aninhada na coleção 'investigacoes': documento principal com referência à não conformidade, e subcoleção 'evidencias' com registros imutáveis

- **Justificativa:** Rastreabilidade e auditabilidade exigem separação clara entre investigação e evidências; subcoleção permite histórico imutável e escalabilidade
- **Alternativas descartadas:** Campo array simples: dificulta versionamento e auditoria; registros separados sem relação: complexidade de query e risco de orfandade
- **Impacto:** Arquitetura | Dados | Performance

### Implementar soft-delete ou flag 'ativo' em evidências em vez de exclusão física, e registrar quem/quando desativou

- **Justificativa:** Restrição de rastreabilidade: auditoria deve ver o histórico completo, incluindo evidências removidas; soft-delete mantém integridade
- **Alternativas descartadas:** Deletar evidência: perde história; não registrar quem removeu: auditoria incompleta
- **Impacto:** Auditoria | Dados

### Campo 'Responsável' em evidência preenchido automaticamente com usuário logado; dropdown para selecionar outro responsável apenas se necessário

- **Justificativa:** Reduz fricção no fluxo; auto-preenchimento com opção de override garante rastreabilidade e flexibilidade (ex: evidência coletada por terceiro)
- **Alternativas descartadas:** Sempre obrigatório selecionar: aumenta clicks; nunca permitir mudança: inflexível se investigador delega coleta
- **Impacto:** UX | Auditoria

### Exibir mensagem clara ('Investigação Concluída') após salvar com sucesso, com opção de visualizar resumo ou retornar à lista de não conformidades

- **Justificativa:** UX clara sobre transição de estado; permite operador confirmar conclusão ou continuar fluxo sem reconfusão
- **Alternativas descartadas:** Redirect automático: pode confundir usuário; sem feedback: usuário duvida se salvou
- **Impacto:** UX

## Plano de Ação Corretiva — `plano-acao-corretiva.html`

Construir um formulário de duas seções: (1) seção somente-leitura exibindo investigação associada e causa raiz confirmada, com guarda-chuva visual indicando que precisa estar 'Concluída'; (2) seção de entrada para criar/atualizar ações corretivas, com campos controlados (ação obrigatória, responsável por dropdown, prazo com validação min=hoje+1d). Adicionar tabela dinâmica listando planos já criados e permitir ao responsável atualizar status e anexar evidência, com rastreamento automático de datas e autoria.

*Arquivo gerado: 44445 caracteres · retrabalhos: 2*


### Implementar guarda lógica: tela só carrega se investigação.status === 'Concluída'

- **Justificativa:** Critério de aceite explícito: 'Tela acessível apenas se investigação associada estiver com status Concluída'
- **Alternativas descartadas:** Exibir tela com aviso: menor segurança de dados, deixa margem para inconsistência; redirecionar: pior UX
- **Impacto:** Arquitetura

### Seção de investigação em card somente-leitura (read-only fields) mostrando: investigação ID, descrição, não-conformidade associada, lote/matéria-prima/equipamento/turno, e causa raiz confirmada

- **Justificativa:** Critério: 'Exibe investigação e causa raiz confirmada em seção somente leitura'; oferece contexto e rastreabilidade obrigatória do cliente
- **Alternativas descartadas:** Omitir card: perderia rastreabilidade; permitir edição: violaria conceito de investigação concluída como imutável
- **Impacto:** UX|Auditoria

### Campo 'Ação Corretiva' como textarea obrigatória (validação cliente + servidor)

- **Justificativa:** Critério: 'Campo Ação Corretiva (texto obrigatório) descreve a ação a tomar'
- **Alternativas descartadas:** Campo simples input: text-area permite descrições mais ricas e auditáveis
- **Impacto:** UX|Dados

### Campo 'Responsável' como dropdown preenchido via query a coleção de usuários da base, com filtro por papéis relevantes (supervisor, operador, gerente)

- **Justificativa:** Critério: 'Campo Responsável (dropdown com usuários da base) define quem executa'; rastreabilidade: cada ação tem dono identificável
- **Alternativas descartadas:** Input livre: impossibilitaria auditoria e rastreamento de quem é responsável; busca autocompletada: mais complexa, dropdown é suficiente
- **Impacto:** Dados|Auditoria

### Campo 'Prazo' como date picker com minDate = hoje + 1 dia (validação cliente + servidor)

- **Justificativa:** Critério: 'Prazo (date picker) com valor mínimo = hoje + 1 dia'; garante realismo do prazo
- **Alternativas descartadas:** Campo de input texto: menor garantia de formato; permite selecionar datas passadas sem validação
- **Impacto:** UX|Dados

### Ao salvar novo plano, gravar automaticamente: id único, investigacao_id (FK), acao_corretiva (texto), responsavel_id, prazo, data_criacao (now), criado_por (current user), status='Aberto', ultima_atualizacao=data_criacao

- **Justificativa:** Critérios: 'Ao salvar novo plano, sistema registra: data criação, responsável pela criação, status inicial Aberto — tudo automaticamente'; 'Histórico de mudanças de status é rastreável'
- **Alternativas descartadas:** Deixar usuário informar data_criacao e criado_por: abre brecha para manipulação; não registrar criado_por: perde auditoria de quem criou
- **Impacto:** Dados|Auditoria

### Tabela dinâmica listando planos criados com colunas: Ação (truncada com tooltip), Responsável (nome), Prazo (data formatada), Status (badge colorida: Aberto=amarela, Concluída=verde), Última Atualização (data+hora), Ações (botões para editar/visualizar detalhes)

- **Justificativa:** Critério: 'Plano criado é exibido em tabela com colunas: Ação, Responsável, Prazo, Status, Última Atualização'
- **Alternativas descartadas:** Lista simples sem tabela: menor clareza visual; colunas adicionais (criado_por): polui tabela, move-se para detalhes do plano
- **Impacto:** UX

### Somente o responsável pela ação (ou admin) pode clicar em 'Atualizar Status' para 'Concluída' e anexar evidência (campo textarea para descrição de conclusão + uploader de arquivo único ou múltiplo com tipos permitidos: PDF, imagem, vídeo)

- **Justificativa:** Critério: 'Responsável pela ação pode atualizar status para Concluída e anexar evidência'; garante que quem fez a ação registre a prova
- **Alternativas descartadas:** Qualquer um pode marcar como concluída: viola rastreabilidade; apenas admin: afasta o responsável, reduz agilidade
- **Impacto:** Arquitetura|Auditoria|UX

### Ao marcar como 'Concluída', registrar automaticamente: status='Concluída', data_conclusao=now, concluido_por=current_user, ultima_atualizacao=now; gerar entrada em log de histórico

- **Justificativa:** Critério: 'Ao marcar como concluída, sistema registra data/hora de conclusão automaticamente'; 'Histórico de mudanças de status é rastreável (data e quem fez)'
- **Alternativas descartadas:** Permitir edição manual de data_conclusao: abre brecha para fraude; não registrar concluido_por: perde rastreabilidade
- **Impacto:** Dados|Auditoria

### Evidência armazenada como sub-documento em plano_acao: { texto_conclusao, arquivo_url, data_upload, enviado_por, turno (if applicable), equipamento (if applicable) }

- **Justificativa:** Restrição cliente: 'Todo registro com evidência auditável: data, responsável, turno e equipamento'; rastreabilidade completa
- **Alternativas descartadas:** Armazenar evidência em coleção separada: fragmentaria rastreabilidade; não registrar turno/equipamento: viola auditoria exigida
- **Impacto:** Dados|Auditoria

### Implementar seção 'Histórico de Status' (abaixo da tabela ou em modal) mostrando: Data/Hora, Status Anterior → Novo Status, Quem Fez, Notas (se houver)

- **Justificativa:** Critério: 'Histórico de mudanças de status é rastreável (data e quem fez)'; restrição: 'rastreabilidade completa'
- **Alternativas descartadas:** Omitir histórico: informação crucial para auditoria se perde; apenas log de servidor: UX opaca
- **Impacto:** UX|Auditoria

### Coleção Firestore: 'planos_acao' com schema: { id, investigacao_id (FK), acao_corretiva, responsavel_id (FK), prazo (Timestamp), data_criacao, criado_por, status, ultima_atualizacao, evidencia { texto, arquivo_url, data_upload, enviado_por, turno, equipamento }, historico: [{ data, usuario, status_anterior, status_novo }] }

- **Justificativa:** Critério: 'Dados são gravados na coleção planos_acao'; estrutura suporta rastreabilidade total exigida pelo cliente
- **Alternativas descartadas:** Armazenar em investigação como sub-coleção: fragmentaria queries; em coleção separada sem FK: quebra integridade referencial
- **Impacto:** Dados|Arquitetura

### Adicionar filtros/busca na tabela: por Status, Responsável, Prazo (range), para facilitar gestão de múltiplos planos

- **Justificativa:** Critério de aceite menciona 'acompanhar'; muitos planos exigem navegação ágil
- **Alternativas descartadas:** Sem filtros: UX piora com crescimento de dados
- **Impacto:** UX|Performance

### Usar componentes React Material-UI (ou similar): Card (seção read-only), TextField, Select (responsável), DatePicker, Button, Table, Dialog (detalhes/edição), FileUpload

- **Justificativa:** Consistência visual com stack existente (assumir React); componentes testados e acessíveis
- **Alternativas descartadas:** HTML puro: maior trabalho de acessibilidade
- **Impacto:** Arquitetura|Acessibilidade

### Implementar validação de acessibilidade: ARIA labels, tab order correto, mensagens de erro claras, contraste de cores, supportar navegação por teclado

- **Justificativa:** Critério de aceite não especifica, mas restrição genérica de conformidade legal (WCAG 2.1 AA)
- **Alternativas descartadas:** Ignorar acessibilidade: risco legal e exclusão de usuários
- **Impacto:** Acessibilidade

## Rastreamento de Lote — `rastreamento-lote.html`

Construir uma tela de consulta com barra de busca em tempo real conectada a um backend otimizado para retornar histórico completo do lote em <2s. O layout segue 4 seções sequenciais (Entrada → Processamento → Conformidade → Expedição) com um painel lateral modal para detalhe de não conformidades. Um cartão de status visual (semáforo) resume a saúde do lote. A exportação PDF é acionada por botão secondary após consulta bem-sucedida, gerando relatório auditável com timestamps e assinatura digital.

*Arquivo gerado: 36926 caracteres · retrabalhos: 2*


### Implementar busca em tempo real (debounce 300ms) contra índice de lotes em banco de dados

- **Justificativa:** Critério de aceite ST-04 exige tempo <2s e busca deve funcionar com ID do lote ou código alternativo. Debounce evita sobrecarga do backend; índice garante resposta rápida. Campo único responsivo atende restrição de simplicidade.
- **Alternativas descartadas:** Busca page-by-page ou dropdown com pré-carregamento de lotes seriam mais lentos e UX inferior. Autocomplete com histórico local sem sincronismo com BD não garante dados atuais.
- **Impacto:** Performance|Arquitetura

### Layout em 4 seções empilhadas (cards) com abas ou scroll vertical — sem abas para evitar cliques extras

- **Justificativa:** ST-04 exige exibição clara de Entrada, Processamento, Conformidade e Expedição. Cards empilhados mantêm contexto visual contínuo e são responsivos (mobile-first). Scroll é mais rápido que abas para usuários em piso de fábrica.
- **Alternativas descartadas:** Abas horizontais requerem mais cliques. Modal único por seção segmentaria demais o fluxo. Dashboard com cards flutuantes perde rastreabilidade visual.
- **Impacto:** UX|Acessibilidade

### Painel lateral (drawer/slide-over) para detalhe de não conformidade, NÃO modal sobreposto

- **Justificativa:** Drawer mantém contexto do lote visível à esquerda enquanto exibe investigação completa à direita. Reduz cliques de navegação. Modal cheia perderia contexto do lote. Atende critério 'clique abre painel lateral'.
- **Alternativas descartadas:** Modal central novo com back-button aumenta carga cognitiva. Expandir inline na tabela limita espaço para conteúdo estruturado da investigação.
- **Impacto:** UX

### Cartão de status visual (semáforo CSS + badge) exibido topo da consulta, antes das 4 seções

- **Justificativa:** Critério ST-04 exige resumo visual em cores (verde/amarelo/vermelho). Posição topo garante visibilidade imediata. Lógica: Verde = nenhuma NC ou todas resolvidas; Amarelo = investigação em andamento; Vermelho = plano ação vencido OU NC crítica. Cálculo ocorre no backend ao trazer histórico.
- **Alternativas descartadas:** Status distribuído por seção confunde interpretação. Ícone pequeno isolado diminui impacto visual.
- **Impacto:** UX|Auditoria

### Tabela de não conformidades com colunas: ID | Defeito | Data Detecção | Origem Detecção | Status Investigação | Status Plano Ação | Ação (clique para drawer)

- **Justificativa:** ST-04 exige exibição de id, descrição, data detecção, origem, status investigação, status plano ação. Tabela é padrão industrial para auditores. Uma coluna 'Ação' com ícone ou botão abre drawer do detalhe. Ordenável por data descendente por padrão.
- **Alternativas descartadas:** Cards de NC individualmente ocupam muito espaço e dificultam comparação. Lista com acordeão é menos eficiente para scan visual.
- **Impacto:** UX|Auditoria

### Mensagem 'Nenhuma não conformidade registrada' em card vazio com ícone check — exibida se array NC vazio

- **Justificativa:** Critério ST-04 explícito. Reduz ansiedade do usuário (não é erro, é bom). Ícone e tipografia clara diferem de erro. Mantém layout consistente.
- **Alternativas descartadas:** Omitir seção de NC: perde transparência. Mostrar tabela vazia confunde.
- **Impacto:** UX

### Botão 'Exportar Relatório' (estado disabled até busca bem-sucedida, depois enabled) gera PDF via backend (não cliente)

- **Justificativa:** ST-06 exige PDF imutável, assinado digitalmente e armazenado 7 anos. Backend controla geração, segurança e arquivamento. Impossível garantir integridade com PDF gerado no cliente. Button state reflete contexto (nenhum lote consultado = disabled).
- **Alternativas descartadas:** PDF gerado em JavaScript (ex: jsPDF) não é auditável: sem assinatura digital real, sem controle de armazenamento de 7 anos, vulnerável a edição. Enviar por email: restrição não solicitada.
- **Impacto:** Auditoria|Segurança|Arquitetura

### PDF contém seções: Cabeçalho (lote, datas, responsáveis) → Cronologia Entrada/Processamento → Tabela NC com datas exatas e operadores → Para cada NC: causa raiz, evidências, plano ação com prazo e conclusão → Rodapé com hash/assinatura digital e timestamp geração

- **Justificativa:** ST-06 exige auditabilidade completa: datas, nomes, turno, equipamento em cada linha — zero agregação. Cronologia garante rastreabilidade. Assinatura digital e hash impedem alteração pós-geração. Armazenamento backend cumpre retenção 7 anos.
- **Alternativas descartadas:** PDF resumido: viola critério auditável. Relatório em página web: não é imutável, não é prova de auditoria.
- **Impacto:** Auditoria|Acessibilidade

### Consulta de dados: 1 call GET /api/lotes/{id} retorna objeto lote completo com arrays correlacionados (nao_conformidades[] com nesting de investigacao e planos_acao)

- **Justificativa:** Tempo <2s exige 1 round-trip máximo. BD com índices em rastreamento_lotes.id + left joins em nao_conformidades e planos_acao (ou document embedding em NoSQL). Evita N+1 queries. Backend retorna JSON estruturado pronto para renderizar as 4 seções.
- **Alternativas descartadas:** Múltiplos calls (lote → NC → planos) = latência >2s. GraphQL sem query optimization pode ser lento se não bem indexed.
- **Impacto:** Performance|Arquitetura|Dados

### UI responsiva mobile-first: barra busca full-width no topo, cards stacked, tabela NC com scroll horizontal em mobile, drawer adaptativo (bottom-sheet em mobile, side-drawer em desktop)

- **Justificativa:** Restrição: 'aplicação responsiva para consulta em qualquer ponto da operação' (piso de fábrica, gerência, atendimento). Mobile-first garante funcionalidade em smartphones/tablets. Bottom-sheet em mobile é padrão UX mobile.
- **Alternativas descartadas:** Desktop-only: inviável para consulta em chão de fábrica. Tabelas não-responsivas com zoom: dificulta leitura.
- **Impacto:** UX|Acessibilidade

### Usar terminologia exata do cliente em labels, placeholders e mensagens: 'não conformidade', 'lote', 'matéria-prima', 'equipamento', 'turno', 'operador', 'causa raiz', 'plano de ação corretiva', 'investigação', 'rastreamento', 'evidência', 'auditável'

- **Justificativa:** Vocabulário padronizado reduz ambiguidade com auditores, gerentes e operadores. Evita traduções imprecisas. Aumenta confiança no sistema (linguagem do domínio).
- **Alternativas descartadas:** Simplificar termos: perde precisão regulatória. Traduzir para EN: usuários BR não entendem.
- **Impacto:** UX|Auditoria

### Timestamps exatos em UTC no banco e exibidos em timezone local do usuário (com indicação, ex: 'São Paulo' ou 'UTC-3')

- **Justificativa:** Auditabilidade exige precisão temporal. UTC no BD garante consistência. Exibir em timezone local melhora UX (gerente vê 14h45 em SP, não 17h45 UTC). Tooltip mostra ambos para auditores.
- **Alternativas descartadas:** Apenas hora local: ambiguidade em auditorias multi-região. Apenas UTC: dificulta interpretação para operadores.
- **Impacto:** Auditoria|UX
