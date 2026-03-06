# Roteiro de Apresentação — Sistema de Compra Programada de Ações

> **Base URL:** `http://localhost:5000`
> **Swagger:** `http://localhost:5000/swagger`
> **Sugestão:** usar o Swagger para as chamadas durante a gravação (visual mais limpo) ou um cliente REST como o Insomnia/Thunder Client.

---

## Antes de começar

### 1. Subir a infraestrutura

```bash
docker-compose up -d
dotnet run --project src/ComprasProgramadas.API
```

### 2. Garantir base limpa (opcional, se já usou antes)

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

---

## BLOCO 1 — Cadastro da Cesta Top Five

> *"Primeiro, o administrador precisa cadastrar a cesta de recomendação — as 5 ações que compõem o portfólio gerido."*

### Chamada

```
POST http://localhost:5000/api/admin/cesta
Content-Type: application/json
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

### Resposta esperada (`201 Created`)

```json
{
  "id": 1,
  "nome": "Top Five — Março 2026",
  "ativa": true,
  "dataCriacao": "2026-03-05T...",
  "itens": [
    { "ticker": "PETR4", "percentual": 30 },
    { "ticker": "VALE3",  "percentual": 25 },
    { "ticker": "ITUB4", "percentual": 20 },
    { "ticker": "BBDC4", "percentual": 15 },
    { "ticker": "WEGE3", "percentual": 10 }
  ]
}
```

> **Destaque:** Validações ativas — a soma dos percentuais deve ser exatamente 100% e a cesta deve ter exatamente 5 ativos. Tente enviar percentuais errados para mostrar o erro de validação.

---

## BLOCO 2 — Cadastro dos Clientes (Adesão ao Produto)

> *"Com a cesta configurada, os clientes podem aderir ao produto. O sistema cria automaticamente uma Conta Gráfica Filhote para cada cliente."*

### Cliente A — Investidor conservador

```
POST http://localhost:5000/api/clientes/adesao
Content-Type: application/json
```

```json
{
  "nome": "Ana Souza",
  "cpf": "11122233344",
  "email": "ana@email.com",
  "valorMensal": 3000.00
}
```

**Resposta esperada (`201 Created`):**
```json
{
  "clienteId": 1,
  "nome": "Ana Souza",
  "numeroConta": "FLH-000001",
  "valorMensal": 3000.00,
  "mensagem": "Adesão realizada com sucesso."
}
```

---

### Cliente B — Investidor moderado

```json
{
  "nome": "Bruno Lima",
  "cpf": "55566677788",
  "email": "bruno@email.com",
  "valorMensal": 6000.00
}
```

---

### Cliente C — Investidor iniciante

```json
{
  "nome": "Carla Neves",
  "cpf": "99988877766",
  "email": "carla@email.com",
  "valorMensal": 1500.00
}
```

> **Destaque:** CPF duplicado é rejeitado. Tente cadastrar o mesmo CPF duas vezes para mostrar a validação.

---

## BLOCO 3 — Execução do Motor de Compra (1ª Rodada)

> *"Chegou o dia 5 do mês — o motor de compra é acionado. Ele consolida os aportes de todos os clientes, compra as ações na conta Master e distribui proporcionalmente para cada filhote."*

### Chamada

```
POST http://localhost:5000/api/motor/executar-compra
Content-Type: application/json
```

```json
{
  "dataReferencia": "2026-03-05"
}
```

### O que acontece internamente (mostrar no vídeo):

```
Total consolidado = R$ 1.000 (Ana) + R$ 2.000 (Bruno) + R$ 500 (Carla) = R$ 3.500

Valor por ativo:
  PETR4: R$ 3.500 × 30% = R$ 1.050
  VALE3: R$ 3.500 × 25% = R$   875
  ITUB4: R$ 3.500 × 20% = R$   700
  BBDC4: R$ 3.500 × 15% = R$   525
  WEGE3: R$ 3.500 × 10% = R$   350

Compra na Master (baseado em cotações do arquivo COTAHIST):
  PETR4: TRUNCAR(1.050 / preço) → X ações
  ... (valores reais dependem da cotação do dia)

Distribuição proporcional:
  Ana   (28,57%): recebe TRUNCAR(28,57% × total de cada ativo)
  Bruno (57,14%): recebe TRUNCAR(57,14% × total de cada ativo)
  Carla (14,29%): recebe TRUNCAR(14,29% × total de cada ativo)

Resíduos → ficam na conta Master para o próximo ciclo
```

### Resposta esperada (estrutura):

```json
{
  "dataExecucao": "2026-03-05T10:00:00Z",
  "totalClientes": 3,
  "totalConsolidado": 3500.00,
  "ordens": [
    {
      "ticker": "PETR4",
      "quantidadeTotal": 28,
      "detalhes": [
        { "mercado": "FRACIONARIO", "ticker": "PETR4F", "quantidade": 28 }
      ],
      "precoUnitario": 37.50,
      "valorTotal": 1050.00
    }
    // ... demais ativos
  ],
  "distribuicoes": [
    {
      "clienteId": 1,
      "nomeCliente": "Ana Souza",
      "aporte": 1000.00,
      "ativos": [
        { "ticker": "PETR4", "quantidade": 8 },
        { "ticker": "VALE3",  "quantidade": 4 }
        // ...
      ]
    }
    // ...
  ],
  "residuos": [
    { "ticker": "PETR4", "quantidade": 1 },
    { "ticker": "WEGE3", "quantidade": 1 }
  ],
  "eventosIRPublicados": 9,
  "mensagem": "Compra programada executada com sucesso para 3 clientes."
}
```

> **Destaque:** Mostrar os `residuos` — são as ações que sobraram da distribuição (truncamento) e ficam na Master para o próximo ciclo. Mostrar também `eventosIRPublicados` — IR dedo-duro de 0,005% calculado para cada compra e publicado no Kafka.

---

## BLOCO 4 — Consulta da Carteira e Rentabilidade

> *"Vamos verificar como ficou a carteira de cada cliente após a primeira compra."*

### Carteira da Ana (clienteId = 1)

```
GET http://localhost:5000/api/clientes/1/carteira
```

**Resposta esperada:**
```json
{
  "clienteId": 1,
  "nomeCliente": "Ana Souza",
  "valorTotalInvestido": 1000.00,
  "valorAtualCarteira": 1023.50,
  "plTotal": 23.50,
  "rentabilidadePercentual": 2.35,
  "ativos": [
    {
      "ticker": "PETR4",
      "quantidade": 8,
      "precoMedio": 37.50,
      "cotacaoAtual": 37.50,
      "valorAtual": 300.00,
      "pl": 0.00,
      "percentualCarteira": 29.32
    }
    // ... demais ativos
  ]
}
```

### Rentabilidade da Ana

```
GET http://localhost:5000/api/clientes/1/rentabilidade
```

**Resposta esperada:**
```json
{
  "clienteId": 1,
  "nomeCliente": "Ana Souza",
  "totalAportado": 1000.00,
  "valorAtualCarteira": 1023.50,
  "plTotal": 23.50,
  "rentabilidadePercentual": 2.35,
  "historicoAportes": [
    {
      "valorAnterior": 0,
      "valorNovo": 3000.00,
      "dataAlteracao": "2026-03-05T..."
    }
  ],
  "ativos": [ ... ]
}
```

---

## BLOCO 5 — Custódia Master (Resíduos)

> *"Podemos ver o saldo de resíduos na conta Master — ações que sobram do truncamento e são aproveitadas na próxima compra."*

```
GET http://localhost:5000/api/admin/conta-master/custodia
```

**Resposta esperada:**
```json
{
  "totalAtivos": 3,
  "custodias": [
    { "ticker": "PETR4", "quantidade": 1, "precoMedio": 37.50 },
    { "ticker": "ITUB4", "quantidade": 1, "precoMedio": 30.00 },
    { "ticker": "WEGE3", "quantidade": 1, "precoMedio": 40.00 }
  ]
}
```

---

## BLOCO 6 — Segunda Rodada do Motor (dia 15)

> *"No dia 15, o motor roda novamente — desta vez já usando os resíduos da Master, comprando apenas o necessário."*

```json
{
  "dataReferencia": "2026-03-16"
}
```

> **Nota:** dia 15/03/2026 é domingo → motor executa dia 16 (segunda-feira). Mostrar que a regra de dia útil está implementada.

> **Destaque:** O campo `residuos` desta execução deve ser menor, pois os resíduos da 1ª rodada foram consumidos (descontados do total a comprar).

---

## BLOCO 7 — Alterar Valor Mensal (RN-013)

> *"Um cliente decide aumentar o aporte. O sistema guarda o histórico do valor anterior."*

```
PUT http://localhost:5000/api/clientes/1/valor-mensal
Content-Type: application/json
```

```json
{
  "novoValorMensal": 6000.00
}
```

**Resposta esperada:**
```json
{
  "clienteId": 1,
  "valorMensalAnterior": 3000.00,
  "valorMensalNovo": 6000.00,
  "mensagem": "Valor mensal atualizado com sucesso."
}
```

> **Destaque:** Consultar a rentabilidade novamente (`GET /api/clientes/1/rentabilidade`) e mostrar o `historicoAportes` com os dois registros: o original de R$ 3.000 e o novo de R$ 6.000.

---

## BLOCO 8 — Mudança de Cesta + Rebalanceamento Automático

> *"O comitê de investimentos decide alterar a composição da cesta. O sistema automaticamente dispara o rebalanceamento para todos os clientes."*

### Nova cesta (BBDC4 e WEGE3 saem; ABEV3 e RENT3 entram)

```
POST http://localhost:5000/api/admin/cesta
Content-Type: application/json
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

### O que acontece automaticamente:

```
Para cada cliente ativo:

  Passo 1 — Vender ativos que SAÍRAM:
    BBDC4: vender 100% da posição → valor vai para o pool de compra
    WEGE3: vender 100% da posição → valor vai para o pool de compra

  Passo 2 — Ajustar ativos MANTIDOS com % alterado (RN-049):
    PETR4: era 30%, agora 25% → over-alocado → vender excesso
    VALE3: era 25%, agora 20% → over-alocado → vender excesso
    ITUB4: manteve 20%       → sem alteração

  Passo 3 — Comprar novos ativos + sub-alocados:
    ABEV3 (20%) e RENT3 (15%) → comprar com o pool arrecadado

  IR sobre vendas:
    Se total de vendas do mês > R$ 20.000 → calcular 20% sobre o lucro
    Publicar evento IR_VENDA no Kafka
```

### Resposta esperada:

```json
{
  "clientesRebalanceados": 3,
  "mensagem": "Rebalanceamento concluído para 3 clientes."
}
```

### Verificar carteira após rebalanceamento

```
GET http://localhost:5000/api/clientes/1/carteira
```

> **Destaque:** A carteira agora deve mostrar ABEV3 e RENT3 no lugar de BBDC4 e WEGE3. PETR4 e VALE3 com quantidade reduzida. ITUB4 inalterado.

---

## BLOCO 9 — Rebalanceamento por Desvio de Proporção (Diferencial — RN-050/051/052)

> *"Ao longo do tempo, a variação de preços distorce as proporções da carteira. O sistema detecta desvios acima de 5 pontos percentuais e reequilibra automaticamente."*

```
POST http://localhost:5000/api/admin/rebalanceamento/desvio?limiar=5
```

**Resposta esperada:**
```json
{
  "clientesRebalanceados": 2,
  "mensagem": "Rebalanceamento por desvio concluído. 2 cliente(s) ajustados."
}
```

> **Nota:** Se nenhum cliente tiver desvio ≥ 5pp neste momento, a resposta retornará `clientesRebalanceados: 0`. Isso é esperado — significa que a carteira está bem alocada. Para demonstrar o desvio, pode-se usar `?limiar=1` (limiar de 1pp, mais sensível).

```
POST http://localhost:5000/api/admin/rebalanceamento/desvio?limiar=1
```

---

## BLOCO 10 — Histórico de Cestas

> *"O sistema mantém o histórico completo de todas as cestas já utilizadas."*

```
GET http://localhost:5000/api/admin/cesta/historico
```

**Resposta esperada:**
```json
{
  "cestas": [
    {
      "id": 1,
      "nome": "Top Five — Março 2026",
      "ativa": false,
      "dataCriacao": "2026-03-05T...",
      "dataDesativacao": "2026-03-05T...",
      "itens": [ ... ]
    },
    {
      "id": 2,
      "nome": "Top Five — Revisão Abril 2026",
      "ativa": true,
      "dataCriacao": "2026-03-05T...",
      "dataDesativacao": null,
      "itens": [ ... ]
    }
  ]
}
```

---

## BLOCO 11 — Saída do Produto (RN-007/008)

> *"Um cliente decide sair do produto. Ele para de participar das compras, mas mantém suas ações em custódia."*

```
POST http://localhost:5000/api/clientes/3/saida
```

**Resposta esperada:**
```json
{
  "clienteId": 3,
  "nome": "Carla Neves",
  "mensagem": "Saída do produto realizada. A posição em custódia foi mantida.",
  "posicaoMantida": true
}
```

> **Destaque:** Executar o motor novamente após a saída da Carla e mostrar que `totalClientes` caiu para 2 — ela não participa mais do agrupamento, mas continua podendo consultar sua carteira.

```
POST http://localhost:5000/api/motor/executar-compra
```
```json
{ "dataReferencia": "2026-03-25" }
```

```
GET http://localhost:5000/api/clientes/3/carteira
```

> A carteira ainda existe — posição mantida.

---

## BLOCO 12 — Kafka: Eventos de IR (Diferencial)

> *"Todas as operações tributáveis são publicadas em tempo real no Kafka."*

Para visualizar as mensagens no Kafka (mostrar no terminal):

```bash
docker exec -it <kafka-container> kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic ir-eventos \
  --from-beginning
```

**Exemplo de mensagem IR Dedo-Duro (compra):**
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

**Exemplo de mensagem IR Venda (rebalanceamento):**
```json
{
  "tipo": "IR_VENDA",
  "clienteId": 2,
  "cpf": "55566677788",
  "mesReferencia": "2026-03",
  "totalVendasMes": 1850.00,
  "lucroLiquido": 245.00,
  "aliquota": 0.0,
  "valorIR": 0.00,
  "dataCalculo": "2026-03-05T10:00:00Z"
}
```

> **Destaque:** `aliquota: 0.0` porque o total de vendas (R$ 1.850) é menor que R$ 20.000 → ISENTO. Mostrar que a regra de isenção está funcionando.

---

## BLOCO 13 — Arquivo COTAHIST (Parser B3)

> *"As cotações são lidas automaticamente do arquivo real da B3, no formato COTAHIST. Não há mock — são preços reais de mercado."*

```
Pasta: cotacoes/
  COTAHIST_D05032026.TXT  ← arquivo diário
  COTAHIST_D04032026.TXT
  COTAHIST_D03032026.TXT
  ...
```

> Mostrar o arquivo brevemente no Explorer. Explicar que o sistema lê o arquivo mais recente, faz o parse posicional (campo `PREULT`, posições 109-121, dividido por 100) e persiste no banco.

---

## Resumo das Funcionalidades Demonstradas

| # | Funcionalidade | Regra | Status |
|---|---|---|---|
| 1 | Cadastro da cesta Top Five | RN-014 a RN-019 | ✅ |
| 2 | Adesão de clientes | RN-001 a RN-006 | ✅ |
| 3 | Motor de compra (dias 5/15/25) | RN-020 a RN-033 | ✅ |
| 4 | Lote padrão vs. fracionário | RN-031/032/033 | ✅ |
| 5 | Distribuição proporcional + truncamento | RN-034 a RN-037 | ✅ |
| 6 | Resíduos na conta Master | RN-039/040 | ✅ |
| 7 | Preço médio ponderado | RN-041 a RN-044 | ✅ |
| 8 | Carteira com P/L e rentabilidade | RN-063 a RN-070 | ✅ |
| 9 | Alterar valor mensal + histórico | RN-011 a RN-013 | ✅ |
| 10 | Saída do produto (mantém custódia) | RN-007/008/009 | ✅ |
| 11 | Mudança de cesta + rebalanceamento automático | RN-045 a RN-049 | ✅ |
| 12 | Rebalanceamento por desvio de proporção | RN-050/051/052 | ✅ |
| 13 | IR Dedo-Duro (0,005% por compra) | RN-053 a RN-056 | ✅ |
| 14 | IR sobre vendas (isenção R$ 20k, 20% lucro) | RN-057 a RN-062 | ✅ |
| 15 | Publicação Kafka (ir-eventos) | RN-055/062 | ✅ |
| 16 | Parser COTAHIST B3 (preços reais) | RN-027 | ✅ |
| 17 | Histórico de cestas | RN-017/018 | ✅ |
| 18 | Regra de dia útil (sáb/dom → seg) | RN-021 | ✅ |

---

## Dica para o Vídeo

**Ordem sugerida de telas (~ 8-10 min):**

1. Mostrar o `docker-compose up -d` e a API subindo (30s)
2. Abrir o Swagger em `http://localhost:5000/swagger` (30s)
3. Bloco 1 — Cadastrar cesta (mostrar validação de erro com 4 itens ou soma ≠ 100%) (1min)
4. Bloco 2 — Cadastrar 3 clientes (1min)
5. Bloco 3 — Motor de compra: destacar o JSON de resposta com ordens, distribuições e resíduos (2min)
6. Bloco 4 — Carteira e rentabilidade de um cliente (1min)
7. Bloco 7 — Alterar valor mensal, mostrar histórico (30s)
8. Bloco 8 — Nova cesta → rebalanceamento automático → mostrar carteira atualizada (2min)
9. Bloco 12 — Consumir tópico Kafka no terminal (30s)
10. Tabela final de funcionalidades (30s)
