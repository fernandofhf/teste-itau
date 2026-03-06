using System.Text.Json;
using ComprasProgramadas.Application.Services.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ComprasProgramadas.Infrastructure.Messaging;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            MessageTimeoutMs = 5000
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task PublicarAsync<T>(string topico, T mensagem, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(mensagem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        try
        {
            var result = await _producer.ProduceAsync(topico, new Message<Null, string> { Value = json }, ct);
            _logger.LogInformation("Mensagem publicada no tópico {Topico}: offset {Offset}", topico, result.Offset);
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, "Erro ao publicar mensagem no tópico {Topico}", topico);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
