namespace ComprasProgramadas.Application.Services.Interfaces;

public interface IKafkaConsumer
{
    /// <summary>
    /// Lê todas as mensagens publicadas no tópico desde o início (offset 0).
    /// Útil para visualização em ambiente de desenvolvimento.
    /// </summary>
    IReadOnlyList<KafkaMensagem> LerTodasMensagens(string topico, int timeoutMs = 2000);

    /// <summary>
    /// Apaga todas as mensagens publicadas no tópico (avança offset até o high watermark).
    /// Retorna o total de mensagens removidas.
    /// </summary>
    Task<int> LimparTopicoAsync(string topico);
}

public record KafkaMensagem(
    string Topico,
    long Offset,
    int Particao,
    DateTime Timestamp,
    string Conteudo);
