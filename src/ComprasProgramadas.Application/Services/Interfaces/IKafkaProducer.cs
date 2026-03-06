namespace ComprasProgramadas.Application.Services.Interfaces;

public interface IKafkaProducer
{
    Task PublicarAsync<T>(string topico, T mensagem, CancellationToken ct = default);
}
