# Sistema de Compra Programada de Ações — Itaú Corretora

## Visão Geral

Sistema backend que automatiza a compra programada de ações para clientes de uma corretora, implementando os princípios de **Clean Architecture**, **CQRS** e **Domain-Driven Design (DDD)**.

### Funcionalidades Principais

| Funcionalidade | Descrição |
|---|---|
| **Adesão de clientes** | Cadastro com CPF, e-mail e valor mensal mínimo de R$ 100 |
| **Cesta de Recomendação** | Portfólio com exatamente 5 ativos cujos percentuais somam 100% |
| **Motor de Compra** | Executa compras programadas nos dias 5, 15 e 25 de cada mês |
| **Rebalanceamento** | Redistribuição automática ao trocar a cesta ativa |
| **Custódia** | Rastreamento de posições individuais e conta master |
| **IR Automático** | Cálculo de dedo-duro (0,005%) e IR sobre vendas (20% acima de R$ 20 mil/mês) |
| **Rentabilidade** | Cálculo de P&L e histórico de aportes por cliente |

---

## Arquitetura

### Camadas (Clean Architecture)

```
┌─────────────────────────────────────────────────┐
│                  API (Controllers)               │   ← Entrada HTTP
├─────────────────────────────────────────────────┤
│            Application (CQRS + Services)         │   ← Regras de negócio
│  Commands / Queries / Handlers / Validators      │
│  Interfaces: IClienteRepository, IKafkaProducer  │
├─────────────────────────────────────────────────┤
│                  Domain (Entidades)               │   ← Núcleo puro
│  Cliente, Custodia, CestaRecomendacao, Cotacao   │
├─────────────────────────────────────────────────┤
│            Infrastructure (Implementações)        │   ← Detalhes técnicos
│  EF Core + MySQL, Kafka, Parser COTAHIST B3      │
└─────────────────────────────────────────────────┘
```

**Regra fundamental:** as camadas internas (Domain, Application) nunca referenciam as externas. A `Application` define interfaces (`IClienteRepository`, `ICotahistService`, `IKafkaProducer`); a `Infrastructure` as implementa.

### Padrões utilizados

- **CQRS** via MediatR 12: Commands (escrita) e Queries (leitura) separados
- **Validação** via FluentValidation 11 com pipeline automático no MediatR
- **ORM** via EF Core 9 com Pomelo (MySQL), migrations code-first
- **Mensageria** via Apache Kafka para eventos de IR (dedo-duro e venda)
- **Background Service** para agendamento diário do motor de compra

### Fluxo do Motor de Compra

```
Dia 5/15/25 (ou próximo dia útil)
     │
     ▼
[1]  Buscar clientes ativos
[2]  Calcular parcela = TRUNCAR(ValorMensal / 3, 2 casas decimais)
[3]  Buscar cesta ativa (5 ativos, soma = 100%)
[4]  Obter cotações via arquivo COTAHIST B3
[5]  Verificar saldo residual na custódia MASTER
[6]  qtd_comprar = TRUNCAR(ValorCesta / Preço) − SaldoMaster
[7]  Separar Lote Padrão (múltiplos de 100) vs Fracionário (+F)
[8]  Atualizar custódia MASTER com novas compras
[9]  Distribuir proporcionalmente para cada cliente (TRUNCAR)
[10] Calcular IR dedo-duro = ValorOperação × 0,005%
[11] Publicar evento de IR no Kafka (tópico: ir-eventos)
[12] Calcular resíduo e manter na conta MASTER para próximo ciclo
```

---

## Tecnologias

| Stack | Versão |
|---|---|
| .NET | 9.0 |
| ASP.NET Core | 9.0 |
| EF Core + Pomelo (MySQL) | 9.0.5 |
| MediatR | 12.4.1 |
| FluentValidation | 11.11.0 |
| Confluent.Kafka | 2.6.0 |
| xUnit + Moq + FluentAssertions | — |
| MySQL | 8.0 (Docker) |
| Apache Kafka | 3.x (Docker) |

---

## Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Git](https://git-scm.com/)

---

## Setup e Execução

### 1. Clonar o repositório

```bash
git clone <url-do-repositorio>
cd teste-itau
```

### 2. Subir a infraestrutura (MySQL + Kafka)

```bash
docker compose up -d
```

Aguarde os serviços ficarem saudáveis (~15 segundos).

### 3. Aplicar migrations do banco de dados

```bash
dotnet ef database update \
  --project src/ComprasProgramadas.Infrastructure \
  --startup-project src/ComprasProgramadas.API
```

Isso cria todas as 10 tabelas e insere a **conta MASTER** (seed automático).

### 4. Configurar variáveis de ambiente (opcional)

O arquivo `src/ComprasProgramadas.API/appsettings.Development.json` já contém os valores padrão para desenvolvimento local:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=compras_programadas;User=root;Password=root;"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "CotacoesPath": "cotacoes"
}
```

### 5. Executar a API

```bash
dotnet run --project src/ComprasProgramadas.API
```

Swagger UI disponível em: `https://localhost:{porta}/swagger`

---

## Endpoints da API

### Clientes

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/clientes/aderir` | Adesão ao produto |
| `POST` | `/api/clientes/{id}/sair` | Saída do produto |
| `PUT` | `/api/clientes/{id}/valor-mensal` | Alterar valor mensal (mín. R$ 100) |
| `GET` | `/api/clientes/{id}/carteira` | Carteira atual do cliente |
| `GET` | `/api/clientes/{id}/rentabilidade` | Rentabilidade e histórico de aportes |

### Administração (Cesta)

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/admin/cestas` | Criar nova cesta (5 ativos, soma=100%) |
| `GET` | `/api/admin/cestas/atual` | Cesta ativa atual |
| `GET` | `/api/admin/cestas/historico` | Histórico de todas as cestas |
| `GET` | `/api/admin/custodia-master` | Custódia consolidada da conta MASTER |

### Motor de Compra

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/api/motor/executar` | Executar compra (body opcional: `{"dataReferencia":"2026-03-05"}`) |

---

## Exemplos de Uso (curl)

### Aderir ao produto

```bash
curl -X POST http://localhost:5000/api/clientes/aderir \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "João Silva",
    "cpf": "12345678901",
    "email": "joao@email.com",
    "valorMensal": 900.00
  }'
```

### Criar cesta de recomendação

```bash
curl -X POST http://localhost:5000/api/admin/cestas \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Top 5 Março 2026",
    "itens": [
      {"ticker": "PETR4", "percentual": 30},
      {"ticker": "VALE3", "percentual": 25},
      {"ticker": "ITUB4", "percentual": 20},
      {"ticker": "BBDC4", "percentual": 15},
      {"ticker": "WEGE3", "percentual": 10}
    ]
  }'
```

### Executar motor de compra

```bash
curl -X POST http://localhost:5000/api/motor/executar \
  -H "Content-Type: application/json" \
  -d '{"dataReferencia": "2026-03-05"}'
```

### Consultar rentabilidade

```bash
curl http://localhost:5000/api/clientes/1/rentabilidade
```

---

## Cotações COTAHIST B3

O sistema lê arquivos no formato **COTAHIST** da B3 para obter preços de fechamento.

1. Baixe o histórico em [B3 - Séries Históricas](https://www.b3.com.br/pt_br/market-data-e-indices/servicos-de-dados/market-data/historico/mercado-a-vista/series-historicas/)
2. Descompacte e coloque os arquivos `.TXT` na pasta definida em `CotacoesPath` (padrão: `cotacoes/`)
3. O motor lerá automaticamente ao executar

**Filtros aplicados no parser:**
- `TIPREG = 01` (cotação diária)
- `CODBDI = 02` (Lote Padrão) ou `96` (ETFs/FIIs)
- Preços convertidos: valor do arquivo ÷ 100 (B3 armazena em centavos)

---

## Executar os Testes

```bash
# Todos os testes (130 testes — 0 falhas)
dotnet test

# Com coleta de cobertura (aplica exclusões via coverlet.runsettings)
dotnet test \
  --collect:"XPlat Code Coverage" \
  --settings coverlet.runsettings \
  --results-directory ./TestResults

# Gerar relatório HTML de cobertura
reportgenerator \
  "-reports:TestResults/**/coverage.cobertura.xml" \
  "-targetdir:TestResults/CoverageReport" \
  "-reporttypes:Html"

# Abrir: TestResults/CoverageReport/index.html
```

### Resultados de cobertura

| Camada | Cobertura de Linhas |
|---|---|
| Application (handlers, validators, services) | 90.3% |
| Domain (entidades de negócio) | 88.2% |
| Infrastructure (repositórios, parsers) | 19% (Kafka/DI excluídos das métricas) |
| **Total (excluindo migrations e DI)** | **88.2%** |

> Exclusões configuradas em `coverlet.runsettings`: migrations EF Core, `AppDbContextFactory`, `DependencyInjection`, projeto API.

---

## Estrutura do Projeto

```
teste-itau/
├── src/
│   ├── ComprasProgramadas.API/           # Controllers + Middleware + DI raiz
│   ├── ComprasProgramadas.Application/   # CQRS + Services + Interfaces
│   │   ├── Commands/
│   │   │   ├── Admin/    (CriarCesta)
│   │   │   ├── Clientes/ (Aderir, Sair, AlterarValorMensal)
│   │   │   └── Motor/    (ExecutarCompra)
│   │   ├── Queries/
│   │   │   ├── Admin/    (GetCestaAtual, GetHistoricoCestas, GetCustodiaMaster)
│   │   │   └── Clientes/ (GetCarteira, GetRentabilidade)
│   │   ├── Services/     (MotorCompraService, RebalanceamentoService)
│   │   └── DTOs/
│   ├── ComprasProgramadas.Domain/        # Entidades + Interfaces de Repositório
│   │   ├── Entities/
│   │   └── Interfaces/
│   └── ComprasProgramadas.Infrastructure/ # EF Core, Kafka, COTAHIST
│       ├── Persistence/
│       │   ├── Context/     (AppDbContext + migrations)
│       │   └── Repositories/
│       ├── Messaging/       (KafkaProducer)
│       └── Cotacoes/        (CotahistParser, CotahistService)
├── tests/
│   ├── ComprasProgramadas.UnitTests/     # 87 testes unitários
│   └── ComprasProgramadas.IntegrationTests/ # 43 testes de integração
├── docs/
├── docker-compose.yml
├── coverlet.runsettings
└── README.md
```

---

## Regras de Negócio Implementadas

### Motor de Compra
| Regra | Descrição |
|---|---|
| **RN-001** | Dias de compra: 5, 15 e 25 (avança para próximo dia útil em fins de semana) |
| **RN-010** | Quantidade = `TRUNCAR(ValorCesta / Preço)` — nunca arredonda para cima |
| **RN-011** | Lote padrão = múltiplos de 100; restante = lote fracionário (ticker+"F") |
| **RN-020** | Distribuição proporcional ao aporte do cliente com TRUNCAR |
| **RN-021** | Resíduo permanece na custódia MASTER para o próximo ciclo de compra |
| **RN-022** | Parcela = `TRUNCAR(ValorMensal / 3 × 100) / 100` |

### Imposto de Renda
| Regra | Descrição |
|---|---|
| **RN-050** | IR dedo-duro em toda compra = `ValorOperação × 0,005%` |
| **RN-051** | IR sobre vendas (rebalanceamento) = `20%` sobre lucro líquido quando vendas mensais > R$ 20.000 |
| **RN-052** | Isento quando há prejuízo ou vendas mensais ≤ R$ 20.000 |

### Rebalanceamento
| Regra | Descrição |
|---|---|
| **RN-030** | Disparado automaticamente ao criar nova cesta ativa |
| **RN-031** | Vende ativos removidos da cesta; usa o valor para comprar os novos |
| **RN-032** | Ajusta percentuais dos ativos mantidos se estiverem acima do alvo |

---

## Decisões de Design

**Por que Clean Architecture?**
Garante que a lógica de negócio (Domain + Application) seja independente de frameworks, banco de dados e serviços externos. Facilita testes unitários puros sem infraestrutura.

**Por que CQRS com MediatR?**
Separa operações de leitura (Queries) de escrita (Commands), simplifica o pipeline de validação e permite escalar independentemente.

**Por que conta MASTER + contas FILHOTE?**
A corretora compra em lote único pela conta MASTER (menor custo operacional) e distribui para as contas individuais dos clientes, mantendo rastreabilidade total e suporte a resíduos.

**Por que TRUNCAR em vez de arredondar?**
Regra de negócio: nunca comprar além do que o valor do cliente cobre. O truncamento garante que o cliente não pague mais do que tem disponível no ciclo.
