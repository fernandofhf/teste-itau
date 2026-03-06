# Roteiro de Apresentação — Sistema de Compra Programada de Ações

> **Entrega item 9.2:** O vídeo deve demonstrar (1) o sistema funcionando, (2) a lógica e raciocínio da implementação e (3) a arquitetura e o porquê das decisões técnicas.

> **Base URL:** `http://localhost:5000`
> **Swagger:** `http://localhost:5000/swagger`
> **Sugestão:** usar o Swagger para as chamadas e o IDE/explorer para mostrar o código.

---

## PARTE 1 — Arquitetura e Decisões Técnicas (~2 min)

> *Abrir o explorer do projeto e comentar a estrutura de pastas enquanto fala.*

### Decisão 1: Clean Architecture + CQRS

O projeto está organizado em 4 camadas:

```
ComprasProgramadas.Domain          ← Entidades e regras de negócio puras
ComprasProgramadas.Application     ← Casos de uso via Commands/Queries (CQRS com MediatR)
ComprasProgramadas.Infrastructure  ← MySQL, Kafka, parser COTAHIST
ComprasProgramadas.API             ← Controllers REST, Swagger, BackgroundService
```

**Por quê?** A separação garante que as regras de negócio (Domain) não dependem de banco ou framework. O CQRS separa operações de escrita (Commands) das de leitura (Queries), facilitando evolução independente e testes.

### Decisão 2: MySQL + EF Core com Migrations

Banco relacional escolhido pela natureza das entidades (Cliente → ContaGrafica → Custodia → Distribuição). Migrations versionam o schema — mostrar pasta `Migrations/`.

### Decisão 3: Kafka para eventos de IR

IR dedo-duro e IR sobre vendas são publicados assincronamente no tópico `ir-eventos`. A corretora (consumidor externo) recebe os eventos sem bloquear o motor de compra.

### Decisão 4: Parser COTAHIST sem dependência externa

Arquivo TXT posicional da B3 lido com `File.ReadLines()` — sem API externa, sem mock. Mostrar a pasta `cotacoes/` com os arquivos reais.

---

## PARTE 2 — Ambiente e Setup (~1 min)

### 1. Subir a infraestrutura

```bash
docker-compose up -d
dotnet run --project src/ComprasProgramadas.API
```

### 2. Garantir base limpa (opcional)

```
DELETE http://localhost:5000/api/admin/reset-database
```

**Resposta esperada:**
```json
{
  "mensagem": "Base zerada com sucesso. Conta Master recriada.",
  "contaMasterId": 1
}
```

> **Explicar:** O seeder recria a Conta Master automaticamente. Ela é a conta da corretora que consolida todas as compras antes da distribuição para os clientes.

---

## PARTE 3 — Demonstração do Sistema Funcionando

---

### BLOCO 1 — Cadastro da Cesta Top Five

> *"O administrador define as 5 ações e os percentuais. Esta é a única cesta vigente — quando alterada, o sistema rebalanceia automaticamente todos os clientes."*

```
POST http://localhost:5000/api/admin/cesta
```

```json
{
  "nome": "Top Five — Março 2026",
  "itens": [
    { "ticker": "PETR4", "percentual": 30 },
    { "ticker": "VALE3",  "percentual": 25 },
    { "ticker": "ITUB4", "percentual": 20 },
    { "ticker": "BBDC4", "percentual": 15 },
    { "ticker": "WEGE3", "percentual": 10 }
  ]
}
```

**Resposta esperada (`201 Created`):**
```json
{
  "id": 1,
  "nome": "Top Five — Março 2026",
  "ativa": true,
  "rebalanceamentoDisparado": false,
  "itens": [
    { "ticker": "PETR4", "percentual": 30 },
    { "ticker": "VALE3",  "percentual": 25 },
    { "ticker": "ITUB4", "percentual": 20 },
    { "ticker": "BBDC4", "percentual": 15 },
    { "ticker": "WEGE3", "percentual": 10 }
  ]
}
```

> **Mostrar validação:** Tente enviar com 4 itens ou soma ≠ 100% — o FluentValidation rejeita antes do handler.

> **Explicar o código:** `CriarCestaValidator` usa FluentValidation. `CriarCestaHandler` desativa a cesta anterior e dispara rebalanceamento se existir — tudo dentro do mesmo Command/Handler (CQRS).

---

### BLOCO 2 — Adesão dos Clientes

> *"Ao aderir, o sistema cria automaticamente uma Conta Gráfica Filhote e, como há uma cesta ativa, já cria as custódias individuais zeradas para cada ativo — implementando a RN-004."*

**Cliente A — R$ 3.000/mês:**
```json
{
  "nome": "Ana Souza",
  "cpf": "11122233344",
  "email": "ana@email.com",
  "valorMensal": 3000.00
}
```

**Cliente B — R$ 6.000/mês:**
```json
{
  "nome": "Bruno Lima",
  "cpf": "55566677788",
  "email": "bruno@email.com",
  "valorMensal": 6000.00
}
```

**Cliente C — R$ 1.500/mês:**
```json
{
  "nome": "Carla Neves",
  "cpf": "99988877766",
  "email": "carla@email.com",
  "valorMensal": 1500.00
}
```

> **Mostrar validação:** Tente cadastrar o mesmo CPF duas vezes — rejeitado com `400 Bad Request`.

> **Explicar o código:** `AderirProdutoHandler` cria em sequência: Cliente → ContaGrafica Filhote → Custódias (uma por ticker da cesta ativa) → HistoricoAporte inicial. Tudo em um único Command.

---

### BLOCO 3 — Motor de Compra (1ª Rodada — Dia 5)

> *"O motor é o coração do sistema. Vou acionar manualmente para simular o dia 5."*

```
POST http://localhost:5000/api/motor/executar-compra
```
```json
{ "dataReferencia": "2026-03-05" }
```

**Explicar o fluxo enquanto a resposta aparece:**

```
1. Busca clientes ativos → Ana (R$1.000/parc), Bruno (R$2.000/parc), Carla (R$500/parc)
   Parcela = TRUNCAR(ValorMensal / 3 * 100) / 100
   Total consolidado = R$ 3.500

2. Lê cotações do arquivo COTAHIST mais recente em cotacoes/
   Ex: PETR4 R$37,50 | VALE3 R$62,00 | ITUB4 R$30,00 | BBDC4 R$15,00 | WEGE3 R$40,00

3. Calcula quantidade a comprar:
   PETR4: TRUNCAR(3500 × 30% / 37,50) = TRUNCAR(28,0) = 28 ações
   VALE3: TRUNCAR(3500 × 25% / 62,00) = TRUNCAR(14,1) = 14 ações
   ... etc

4. Verifica saldo master (resíduos de rodadas anteriores) — desconta da compra

5. Separa lote padrão vs fracionário (RN-031/032/033):
   28 ações de PETR4 → 0 lotes padrão + 28 fracionárias (PETR4F)
   Se fossem 350: 3 lotes (PETR4) + 50 fracionárias (PETR4F)

6. Distribui proporcionalmente com TRUNCAR (RN-036):
   Ana   (28,57%): TRUNCAR(28 × 28,57%) = 8 ações de PETR4
   Bruno (57,14%): TRUNCAR(28 × 57,14%) = 16 ações de PETR4
   Carla (14,29%): TRUNCAR(28 × 14,29%) = 4 ações de PETR4
   Distribuídas: 8+16+4 = 28 → resíduo = 0

7. Calcula preço médio de cada custódia filhote:
   PM = (QtdAnt × PMAnt + QtdNova × PrecoNova) / (QtdAnt + QtdNova)

8. Calcula IR dedo-duro por distribuição:
   Ana PETR4: 8 × R$37,50 = R$300 → IR = R$300 × 0,005% = R$0,01
   Publica no Kafka tópico ir-eventos

9. Resíduos permanecem na Master para o próximo ciclo
```

> **Destaque no JSON de resposta:** mostrar `ordens`, `distribuicoes`, `residuos`, `eventosIRPublicados`.

---

### BLOCO 4 — Carteira e Rentabilidade

> *"Verificando como ficou a carteira da Ana após a primeira compra."*

```
GET http://localhost:5000/api/clientes/1/carteira
GET http://localhost:5000/api/clientes/1/rentabilidade
```

> **Explicar:** A cotação atual é buscada do banco (última do COTAHIST). P/L = (CotaçãoAtual - PreçoMédio) × Quantidade. Composição % mostra o peso de cada ativo na carteira total.

---

### BLOCO 5 — Resíduos na Conta Master

> *"Os resíduos do truncamento ficam na Master e serão aproveitados na próxima compra — economia real de dinheiro para a corretora."*

```
GET http://localhost:5000/api/admin/conta-master/custodia
```

---

### BLOCO 6 — Segunda Rodada (Dia 15 → cai no domingo → executa dia 16)

```json
{ "dataReferencia": "2026-03-16" }
```

> **Explicar:** `IsDataCompra()` verifica se a data é o próximo dia útil após os dias 5/15/25. `CalcularProximoDiaUtil()` avança sábado/domingo para segunda. A compra desta rodada desconta os resíduos da master — quantidade comprada externamente será menor.

---

### BLOCO 7 — Alterar Valor Mensal + Histórico (RN-011/013)

```
PUT http://localhost:5000/api/clientes/1/valor-mensal
```
```json
{ "novoValorMensal": 6000.00 }
```

```
GET http://localhost:5000/api/clientes/1/historico-aportes
```

> **Explicar:** `AlterarValorMensalHandler` salva na tabela `HistoricoAporte` o valor anterior, o novo e a data — rastreabilidade completa conforme RN-013. A partir da próxima rodada do motor, o novo valor já é usado.

---

### BLOCO 8 — Mudança de Cesta + Rebalanceamento Automático (RN-045/049)

> *"O comitê altera a cesta. BBDC4 e WEGE3 saem; ABEV3 e RENT3 entram. O rebalanceamento é disparado automaticamente para todos os clientes ativos."*

```
POST http://localhost:5000/api/admin/cesta
```
```json
{
  "nome": "Top Five — Revisão Abril 2026",
  "itens": [
    { "ticker": "PETR4", "percentual": 25 },
    { "ticker": "VALE3",  "percentual": 20 },
    { "ticker": "ITUB4", "percentual": 20 },
    { "ticker": "ABEV3", "percentual": 20 },
    { "ticker": "RENT3", "percentual": 15 }
  ]
}
```

**Explicar o que acontece internamente (`RebalanceamentoService`):**

```
Para cada cliente ativo:

  Passo 1 — Vender ativos que SAÍRAM da cesta (RN-046/047):
    BBDC4: vender 100% → valor vai para o pool de compra
    WEGE3: vender 100% → valor vai para o pool de compra

  Passo 2 — Ajustar ativos MANTIDOS com % alterado (RN-049):
    PETR4: era 30%, agora 25% → over-alocado → calcular excesso e vender
    VALE3: era 25%, agora 20% → over-alocado → calcular excesso e vender
    ITUB4: manteve 20%        → sem alteração

  Passo 3 — Comprar novos ativos proporcionalmente (RN-048):
    ABEV3 (20/35 = 57%) e RENT3 (15/35 = 43%) do pool arrecadado

  IR sobre vendas (RN-057/059):
    Se total vendas no mês ≤ R$20.000 → ISENTO
    Se total vendas no mês > R$20.000 → 20% sobre lucro líquido
    Publica IR_VENDA no Kafka
```

**Verificar carteira após rebalanceamento:**
```
GET http://localhost:5000/api/clientes/1/carteira
```
> A carteira agora mostra ABEV3 e RENT3 no lugar de BBDC4/WEGE3. PETR4/VALE3 com quantidade reduzida. ITUB4 inalterado.

---

### BLOCO 9 — Rebalanceamento por Desvio de Proporção (RN-050/051/052 — Diferencial)

> *"Com o tempo, a valorização diferente de cada ativo distorce as proporções. O sistema detecta desvios acima do limiar e reequilibra."*

```
POST http://localhost:5000/api/admin/rebalanceamento/desvio?limiar=5
```

> **Explicar:** Para cada ativo, calcula `percentualAtual = (valorAtivo / valorTotal) × 100`. Se `|percentualAtual - percentualCesta| ≥ limiar`, o cliente precisa de ajuste. Over-alocados são vendidos; sub-alocados são comprados com o valor arrecadado.

> Se nenhum cliente tiver desvio ≥ 5pp (carteira bem alocada), use `?limiar=1` para forçar a demonstração.

---

### BLOCO 10 — Histórico de Ordens por Cliente

> *"Toda compra e venda gerada pelo motor ou rebalanceamento fica rastreada por cliente — útil para auditoria e para o avaliador verificar o que foi executado."*

```
GET http://localhost:5000/api/clientes/1/ordens
```

> **Explicar:** `HistoricoOrdemCliente` registra tipo (Compra/Venda), ticker, quantidade, preço unitário e origem (MotorCompra / RebalanceamentoCesta / RebalanceamentoDesvio).

---

### BLOCO 11 — Saída do Produto (RN-007/008/009)

> *"A Carla decide sair. Ela para de participar das compras, mas mantém suas ações em custódia — posição preservada."*

```
POST http://localhost:5000/api/clientes/3/saida
```

> **Demonstrar:** Rodar o motor após a saída e mostrar que `totalClientes = 2`. Consultar carteira da Carla e mostrar que os ativos ainda estão lá.

```json
{ "dataReferencia": "2026-03-25" }
```
```
GET http://localhost:5000/api/clientes/3/carteira
```

---

### BLOCO 12 — Kafka: Eventos de IR

> *"Todas as operações tributáveis são publicadas em tempo real no Kafka."*

```bash
docker exec -it <kafka-container> kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic ir-eventos \
  --from-beginning
```

**Mensagem IR Dedo-Duro:**
```json
{
  "tipo": "IR_DEDO_DURO",
  "clienteId": 1,
  "cpf": "11122233344",
  "ticker": "PETR4",
  "tipoOperacao": "COMPRA",
  "quantidade": 8,
  "precoUnitario": 37.50,
  "valorOperacao": 300.00,
  "aliquota": 0.00005,
  "valorIR": 0.02,
  "dataOperacao": "2026-03-05T10:00:00Z"
}
```

**Mensagem IR Venda (isento — vendas < R$20k):**
```json
{
  "tipo": "IR_VENDA",
  "clienteId": 1,
  "cpf": "11122233344",
  "mesReferencia": "2026-03",
  "totalVendasMes": 1850.00,
  "lucroLiquido": 245.00,
  "aliquota": 0.0,
  "valorIR": 0.00,
  "dataCalculo": "2026-03-05T10:00:00Z"
}
```

> **Explicar:** `aliquota: 0.0` porque total de vendas < R$20.000 → isento. Se fosse acima, seria `aliquota: 0.20` sobre o lucro líquido.

---

### BLOCO 13 — Parser COTAHIST B3 (preços reais)

> *"Nenhum mock de cotação — os preços vêm de arquivos reais da B3."*

Mostrar a pasta `cotacoes/` no explorer. Explicar o parse:

```
Arquivo: COTAHIST_D{YYYYMMDD}.TXT — formato posicional, 245 chars/linha, ISO-8859-1

Campos lidos:
  TIPREG (pos 1-2):   filtrar "01" (registro de cotação)
  CODBDI (pos 11-12): filtrar "02" (lote) ou "96" (fracionário)
  CODNEG (pos 13-24): ticker — ex: "PETR4       "
  TPMERC (pos 25-27): filtrar "010" (vista) ou "020" (fracionário)
  PREULT (pos 109-121): preço de fechamento ÷ 100 = valor em R$

O sistema lê o arquivo mais recente da pasta, filtra apenas os 5 tickers
da cesta ativa e persiste na tabela Cotacoes para consulta rápida.
```

---

## PARTE 4 — Testes e Qualidade (~1 min)

> *"Cobertura mínima exigida: 70%."*

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Mostrar brevemente os arquivos de teste:

- `MotorCompraServiceTests`: valida distribuição proporcional, resíduos, COTAHIST mock
- `RebalanceamentoServiceTests`: valida venda de ativos que saíram, compra dos novos, IR
- `ClienteCommandsTests`: adesão, saída, alteração de valor mensal
- `QueryHandlerTests`: carteira, rentabilidade, custódia master

> **Explicar:** Testes de integração usam SQLite in-memory (sem MySQL real), o que permite rodar sem Docker.

---

## PARTE 5 — Diferenciais Implementados (~30s)

| Diferencial | Status |
|---|---|
| Frontend web (HTML/JS puro) para testar todos os endpoints | ✅ |
| CQRS com MediatR + Clean Architecture | ✅ |
| DDD (entidades com comportamento, sem setters públicos) | ✅ |
| Logs estruturados com Serilog | ✅ |
| Rebalanceamento por desvio de proporção (RN-050/051/052) | ✅ |
| Histórico de ordens por cliente para auditoria | ✅ |
| Histórico de alterações do valor mensal | ✅ |

---

## Resumo das Funcionalidades — Tabela Final

| # | Funcionalidade | Regra | Status |
|---|---|---|---|
| 1 | Cadastro da cesta Top Five com validação | RN-014 a RN-019 | ✅ |
| 2 | Adesão de clientes + conta filhote + custódias | RN-001 a RN-006 | ✅ |
| 3 | Motor de compra (dias 5/15/25, regra dia útil) | RN-020 a RN-033 | ✅ |
| 4 | Lote padrão vs. fracionário (sufixo F) | RN-031/032/033 | ✅ |
| 5 | Distribuição proporcional com TRUNCAR | RN-034 a RN-037 | ✅ |
| 6 | Resíduos na conta Master reutilizados | RN-039/040 | ✅ |
| 7 | Preço médio ponderado por compra | RN-041 a RN-044 | ✅ |
| 8 | Carteira com P/L, rentabilidade e composição % | RN-063 a RN-070 | ✅ |
| 9 | Alterar valor mensal + histórico | RN-011 a RN-013 | ✅ |
| 10 | Saída do produto (soft delete, custódia mantida) | RN-007/008/009 | ✅ |
| 11 | Mudança de cesta + rebalanceamento automático | RN-045 a RN-049 | ✅ |
| 12 | Rebalanceamento por desvio de proporção | RN-050/051/052 | ✅ |
| 13 | IR Dedo-Duro (0,005% por compra distribuída) | RN-053 a RN-056 | ✅ |
| 14 | IR sobre vendas (isenção R$20k / 20% lucro) | RN-057 a RN-062 | ✅ |
| 15 | Publicação Kafka (ir-eventos) | RN-055/062 | ✅ |
| 16 | Parser COTAHIST B3 — preços reais de mercado | RN-027 | ✅ |
| 17 | Histórico de cestas | RN-017/018 | ✅ |
| 18 | Histórico de ordens por cliente | Diferencial | ✅ |
| 19 | Frontend web para demonstração | Diferencial | ✅ |

---

## Ordem Sugerida para o Vídeo (~8-10 min)

| Tempo | Bloco |
|---|---|
| 0:00 – 0:30 | `docker-compose up -d` + API subindo |
| 0:30 – 2:00 | **PARTE 1** — Arquitetura: mostrar estrutura de pastas, explicar Clean Architecture + CQRS, Kafka, COTAHIST |
| 2:00 – 3:00 | **Blocos 1 e 2** — Cesta + 3 clientes (mostrar validações) |
| 3:00 – 5:00 | **Bloco 3** — Motor de compra: explicar o fluxo dos 9 passos, mostrar JSON de resposta |
| 5:00 – 5:30 | **Bloco 4** — Carteira + rentabilidade |
| 5:30 – 6:00 | **Bloco 7** — Alterar valor mensal + histórico |
| 6:00 – 7:30 | **Bloco 8** — Nova cesta → rebalanceamento → carteira atualizada |
| 7:30 – 8:00 | **Bloco 12** — Kafka: mostrar mensagens no terminal |
| 8:00 – 8:30 | **Bloco 13** — Arquivo COTAHIST no explorer |
| 8:30 – 9:00 | **PARTE 4** — Testes (`dotnet test`) |
| 9:00 – 9:30 | Tabela final de funcionalidades |
