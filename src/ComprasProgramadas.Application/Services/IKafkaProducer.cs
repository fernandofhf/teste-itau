namespace ComprasProgramadas.Application.Services;

public interface IKafkaProducer
{
    Task PublicarAsync<T>(string topico, T mensagem, CancellationToken ct = default);
}
