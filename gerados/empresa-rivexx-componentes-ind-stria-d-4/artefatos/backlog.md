# Backlog — Rastreador de Não Conformidades

`run empresa-rivexx-componentes-ind-stria-d-4-a37eda` · **PO Agent** · cliente: Rivexx Componentes

> Centraliza registro, investigação e rastreamento de não conformidades na produção, permitindo análise de causa raiz estruturada e recuperação instantânea do histórico de lotes.


**Vocabulário do cliente:** não conformidade, lote, matéria-prima, equipamento, turno, operador, causa raiz, plano de ação corretiva, investigação, rastreamento, evidência, auditável, origem da detecção


**Regras de negócio extraídas do briefing**

- Toda não conformidade deve registrar obrigatoriamente: data, turno, operador, equipamento, lote afetado e matéria-prima
- Investigação deve ser iniciada dentro de 24h da detecção da não conformidade
- Plano de ação só pode ser criado após investigação concluída com causa raiz confirmada
- Rastreamento de lote deve cobrir entrada de insumo até expedição do produto final
- Qualquer alteração em não conformidade, investigação ou plano de ação registra data, responsável e turno
- Múltiplas não conformidades podem afetar o mesmo lote

## Tela: Registro de Não Conformidade (`registro-nao-conformidade`, formulario)

Capturar imediatamente os dados de uma não conformidade detectada, com contexto completo de operação e turno, sem necessidade de navegação complexa


### ST-01 — Registrar não conformidade no ponto de origem com contexto completo

**Prioridade:** Alta

Como operador de chão de fábrica, quero registrar uma não conformidade detectada sem sair do fluxo de produção, para que a informação seja capturada imediatamente e sem perda de contexto.

**Critérios de aceite**

- [ ] Formulário acessível por celular em interface responsiva, com campos visíveis sem zoom ou scroll horizontal
- [ ] Campos obrigatórios: data (pré-preenchida com hoje), turno (seletor com opções 1/2/3), operador (nome ou matrícula do usuário logado), equipamento (dropdown com lista de equipamentos da planta), lote (entrada de texto ou scanning), matéria-prima (dropdown), descrição do defeito (campo texto)
- [ ] Sistema registra automaticamente data/hora, usuário logado e turno no momento do envio — sem campo manual para estes dados
- [ ] Ao submeter, retorna ID único da não conformidade e mensagem de sucesso visível
- [ ] Dados são gravados em tempo real na coleção 'nao_conformidades'
- [ ] Formulário volta a estado vazio após envio bem-sucedido, pronto para próximo registro

**Restrições do briefing atendidas**

- Aplicação responsiva, operadores registram pelo celular no chão de fábrica
- Interface operável sem treinamento técnico
- Todo registro com evidência auditável: data, responsável, turno e equipamento

### ST-05 — Registrar não conformidade offline e sincronizar ao restaurar conexão

**Prioridade:** Média

Como operador em área com conectividade intermitente, quero registrar não conformidade mesmo sem conexão, garantindo que o registro seja persistido quando a rede retornar.

**Critérios de aceite**

- [ ] Aplicação detecta perda de conectividade e exibe indicador visual na interface (ícone de conexão/wifi)
- [ ] Formulário de registro permanece totalmente funcional offline
- [ ] Ao submeter formulário offline, dados são armazenados localmente (localStorage ou IndexedDB)
- [ ] Interface exibe mensagem 'Registrado localmente. Será sincronizado automaticamente.' em tom de sucesso
- [ ] Ao restaurar conexão, sistema detecta automaticamente e inicia sincronização de registros pendentes
- [ ] Durante sincronização, usuário vê progresso visual (spinner ou contador de registros sincronizados)
- [ ] Após sincronização bem-sucedida, registros locais são deletados e usuário recebe notificação de sucesso
- [ ] Se sincronização falhar, dados permanecem locais e usuário pode tentar manualmente via botão 'Sincronizar Agora'
- [ ] Não há duplicação de registros mesmo se sincronização ocorrer múltiplas vezes

**Restrições do briefing atendidas**

- Operadores registram pelo celular no chão de fábrica (onde conectividade pode ser intermitente)

## Tela: Investigação de Causa Raiz (`investigacao-causa-raiz`, calculadora)

Conduzir análise estruturada de uma não conformidade, documentar evidências e confirmar a causa raiz com metodologia padronizada


### ST-02 — Conduzir investigação estruturada de causa raiz

**Prioridade:** Alta

Como analista de qualidade, quero realizar uma investigação estruturada de uma não conformidade registrada, documentando hipóteses, evidências e confirmando causa raiz, para que a análise seja reproduzível e auditável.

**Critérios de aceite**

- [ ] Tela exibe não conformidade selecionada (lote, defeito, equipamento, operador, turno) em seção somente leitura no topo
- [ ] Campo 'Metodologia' apresenta dropdown com opções pré-definidas (ex: '5 Porquês', 'Diagrama de Fishbone', 'Análise FMEA') — operador seleciona
- [ ] Campo 'Hipótese de Causa' (texto livre) permite documentar suspeita inicial
- [ ] Seção 'Evidências' permite registrar múltiplas evidências (data, tipo: inspeção visual / teste / entrevista / consulta registro), cada uma com descrição e responsável
- [ ] Ao adicionar evidência, esta é gravada imediatamente na coleção 'investigacoes' e exibida em lista abaixo
- [ ] Campo 'Causa Raiz Confirmada' (dropdown: Sim / Não / Pendente) — ao selecionar 'Sim', libera campo de narrativa final
- [ ] Ao marcar como concluída (Sim na causa raiz), sistema registra data/hora e analista automaticamente
- [ ] Botão 'Salvar Investigação' persiste status como 'Concluída' apenas se causa raiz estiver confirmada

**Restrições do briefing atendidas**

- Metodologia estruturada para análise de causa raiz
- Todo registro com evidência auditável: data, responsável, turno e equipamento
- Rastreabilidade de todas as alterações

## Tela: Plano de Ação Corretiva (`plano-acao-corretiva`, formulario)

Registrar ações corretivas vinculadas a investigação concluída, definir responsável e prazo, e acompanhar evidência de conclusão


### ST-03 — Criar e acompanhar plano de ação corretiva com evidência de conclusão

**Prioridade:** Alta

Como gerente de qualidade, quero registrar ações corretivas vinculadas a uma investigação concluída, definir responsável e prazo, e acompanhar evidência de cumprimento, para que planos de ação se tornem compromissos rastreáveis.

**Critérios de aceite**

- [ ] Tela acessível apenas se investigação associada estiver com status 'Concluída'
- [ ] Exibe investigação e causa raiz confirmada em seção somente leitura
- [ ] Campo 'Ação Corretiva' (texto obrigatório) descreve a ação a tomar
- [ ] Campo 'Responsável' (dropdown com usuários da base) define quem executa
- [ ] Campo 'Prazo' (data picker) com valor mínimo = hoje + 1 dia
- [ ] Ao salvar novo plano, sistema registra: data criação, responsável pela criação, status inicial 'Aberto' — tudo automaticamente
- [ ] Plano criado é exibido em tabela com colunas: Ação, Responsável, Prazo, Status, Última Atualização
- [ ] Responsável pela ação pode atualizar status para 'Concluída' e anexar evidência (texto ou arquivo)
- [ ] Ao marcar como concluída, sistema registra data/hora de conclusão automaticamente
- [ ] Dados são gravados na coleção 'planos_acao'
- [ ] Histórico de mudanças de status é rastreável (data e quem fez)

**Restrições do briefing atendidas**

- Planos de ação monitorados com evidência
- Todo registro com evidência auditável: data, responsável, turno e equipamento
- Rastreabilidade completa

## Tela: Rastreamento de Lote (`rastreamento-lote`, consulta)

Pesquisar qualquer lote e recuperar seu histórico completo — matéria-prima de origem, equipamentos usados, operadores envolvidos, não conformidades associadas, status de qualidade, destino final — em segundos


### ST-04 — Rastrear lote completo da entrada à expedição em segundos

**Prioridade:** Alta

Como gerente de qualidade ou atendente de cliente, quero pesquisar qualquer lote e acessar seu histórico completo — origem, processamento, não conformidades, status final — em segundos, para responder rápido a clientes e tomar decisões sobre recall.

**Critérios de aceite**

- [ ] Campo de busca (barra única, responsiva) aceita ID do lote ou código alternativo — busca em tempo real
- [ ] Ao localizar lote, exibe:  Seção 1 (Entrada): matéria-prima, data entrada, fornecedor;  Seção 2 (Processamento): equipamentos usados (lista), operadores por turno, datas de processamento;  Seção 3 (Conformidade): lista de todas não conformidades associadas a este lote com links para detalhe;  Seção 4 (Expedição): data saída, destino, status qualidade final
- [ ] Para cada não conformidade listada, exibe: id, descrição do defeito, data detecção, origem detecção, status investigação, status plano ação
- [ ] Clique em não conformidade abre painel lateral com investigação completa (causa raiz, evidências, plano ação)
- [ ] Se lote não teve não conformidades, exibe mensagem clara 'Nenhuma não conformidade registrada'
- [ ] Relatório visual (resumo em verde/amarelo/vermelho) indica status geral do lote: Verde (sem não conformidades ou resolvidas), Amarelo (investigação em andamento), Vermelho (plano ação vencido ou não conformidade crítica)
- [ ] Dados lidos da coleção 'rastreamento_lotes' com correlação em 'nao_conformidades' e 'planos_acao'
- [ ] Tempo de resposta: busca e exibição < 2 segundos

**Restrições do briefing atendidas**

- Rastreabilidade de lote cobrindo toda a cadeia produtiva
- Recuperação de histórico em segundos (vs. horas)
- Aplicação responsiva para consulta em qualquer ponto da operação

### ST-06 — Exportar relatório auditável de lote com histórico completo

**Prioridade:** Média

Como responsável de certificação/auditoria, quero gerar relatório de qualquer lote incluindo todas não conformidades, investigações e planos de ação, com timestamps e responsáveis, para atender auditores trimestrais.

**Critérios de aceite**

- [ ] Após rastrear um lote (ST-04), botão 'Exportar Relatório' está visível
- [ ] Ao clicar, relatório em PDF é gerado e baixado com nome 'Lote_[ID]_[Data].pdf'
- [ ] PDF contém: Cabeçalho com lote, datas, responsáveis;  Histórico cronológico de entrada → processamento → não conformidades com datas exatas;  Para cada não conformidade: descrição, data detecção, operador, equipamento, turno, investigação com causa raiz e data conclusão, planos ação com responsável, prazo, evidência, data conclusão;  Rodapé com data/hora de geração do relatório e assinatura digital do gerenciador
- [ ] Relatório é auditável: todas as datas, nomes e turno aparecem explicitamente — nenhuma informação é resumida ou agregada
- [ ] PDF gerado é imutável (não editável) e armazenado por 7 anos em repositório seguro para conformidade com auditoria trimestral

**Restrições do briefing atendidas**

- Todo registro com evidência auditável: data, responsável, turno e equipamento
- Certificada, auditada trimestralmente
