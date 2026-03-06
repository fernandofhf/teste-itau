using ComprasProgramadas.Application.Services.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ComprasProgramadas.Infrastructure.Messaging;

public class KafkaConsumer : IKafkaConsumer
{
    private readonly string _bootstrapServers;
    private readonly ILogger<KafkaConsumer> _logger;

    public KafkaConsumer(IConfiguration configuration, ILogger<KafkaConsumer> logger)
    {
        _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _logger = logger;
    }

    public IReadOnlyList<KafkaMensagem> LerTodasMensagens(string topico, int timeoutMs = 2000)
    {
        var mensagens = new List<KafkaMensagem>();

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = $"api-reader-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnablePartitionEof = true
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

        // Obter metadados do tópico via AdminClient
        var adminConfig = new AdminClientConfig { BootstrapServers = _bootstrapServers };
        using var adminClient = new AdminClientBuilder(adminConfig).Build();

        Metadata topicMeta;
        try
        {
            topicMeta = adminClient.GetMetadata(topico, TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível obter metadados do tópico {Topico}", topico);
            return mensagens;
        }

        if (topicMeta.Topics.Count == 0 || topicMeta.Topics[0].Error.IsError)
            return mensagens;

        // QueryWatermarkOffsets consulta o broker — GetWatermarkOffsets usa apenas cache local (sempre vazio)
        var assignments = new List<TopicPartitionOffset>();
        var highWatermarks = new Dictionary<int, long>();

        foreach (var p in topicMeta.Topics[0].Partitions)
        {
            var tp = new TopicPartition(topico, p.PartitionId);
            var wm = consumer.QueryWatermarkOffsets(tp, TimeSpan.FromSeconds(5));
            highWatermarks[p.PartitionId] = wm.High;

            // Só atribuir partições que têm mensagens
            if (wm.High > 0)
                assignments.Add(new TopicPartitionOffset(topico, p.PartitionId, Offset.Beginning));
        }

        if (!assignments.Any())
            return mensagens;

        consumer.Assign(assignments);

        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        var eofPartitions = new HashSet<int>();

        while (eofPartitions.Count < assignments.Count)
        {
            var result = consumer.Consume(timeout);

            if (result == null)
                break; // timeout sem atividade

            if (result.IsPartitionEOF)
            {
                eofPartitions.Add(result.Partition.Value);
                continue;
            }

            mensagens.Add(new KafkaMensagem(
                result.Topic,
                result.Offset.Value,
                result.Partition.Value,
                result.Message.Timestamp.UtcDateTime,
                result.Message.Value));

            // Parar se já chegamos ao high watermark de todas as partições
            if (assignments.All(a =>
                mensagens.Where(m => m.Particao == a.Partition.Value)
                         .Select(m => m.Offset)
                         .DefaultIfEmpty(-1)
                         .Max() >= highWatermarks[a.Partition.Value] - 1))
                break;
        }

        _logger.LogInformation("Lidas {Total} mensagens do tópico {Topico}", mensagens.Count, topico);
        return mensagens;
    }

    public async Task<int> LimparTopicoAsync(string topico)
    {
        var adminConfig = new AdminClientConfig { BootstrapServers = _bootstrapServers };
        using var adminClient = new AdminClientBuilder(adminConfig).Build();

        Metadata topicMeta;
        try
        {
            topicMeta = adminClient.GetMetadata(topico, TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível obter metadados do tópico {Topico}", topico);
            return 0;
        }

        if (topicMeta.Topics.Count == 0 || topicMeta.Topics[0].Error.IsError)
            return 0;

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = $"api-cleaner-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();

        var recordsToDelete = new List<TopicPartitionOffset>();
        var totalMensagens = 0L;

        foreach (var p in topicMeta.Topics[0].Partitions)
        {
            var tp = new TopicPartition(topico, p.PartitionId);
            var wm = consumer.QueryWatermarkOffsets(tp, TimeSpan.FromSeconds(5));
            if (wm.High > wm.Low)
            {
                totalMensagens += wm.High - wm.Low;
                recordsToDelete.Add(new TopicPartitionOffset(topico, p.PartitionId, wm.High));
            }
        }

        if (recordsToDelete.Count == 0)
            return 0;

        await adminClient.DeleteRecordsAsync(recordsToDelete);

        _logger.LogInformation("Limpas {Total} mensagens do tópico {Topico}", totalMensagens, topico);
        return (int)totalMensagens;
    }
}
