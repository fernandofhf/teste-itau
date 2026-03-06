using ComprasProgramadas.Application.Services;
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
            // GroupId único por chamada para não persistir offset entre requisições
            GroupId = $"api-reader-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            // Não registrar o consumer group no broker — leitura stateless
            EnablePartitionEof = true
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

        // Buscar partições disponíveis no tópico
        var metadata = consumer.GetWatermarkOffsets(new TopicPartition(topico, new Partition(0)));

        // Atribuir manualmente a partir do offset 0 em todas as partições
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

        var partitions = topicMeta.Topics[0].Partitions
            .Select(p => new TopicPartitionOffset(topico, p.PartitionId, Offset.Beginning))
            .ToList();

        consumer.Assign(partitions);

        // Calcular quantas mensagens existem ao total para saber quando parar
        long totalMensagens = 0;
        foreach (var partition in topicMeta.Topics[0].Partitions)
        {
            var watermarks = consumer.GetWatermarkOffsets(new TopicPartition(topico, partition.PartitionId));
            totalMensagens += watermarks.High - watermarks.Low;
        }

        if (totalMensagens == 0)
            return mensagens;

        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        var eofPartitions = new HashSet<int>();
        var totalPartitions = topicMeta.Topics[0].Partitions.Count;

        while (eofPartitions.Count < totalPartitions)
        {
            var result = consumer.Consume(timeout);

            if (result == null)
                break; // timeout sem mensagem nova

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
        }

        _logger.LogInformation("Lidas {Total} mensagens do tópico {Topico}", mensagens.Count, topico);
        return mensagens;
    }
}
