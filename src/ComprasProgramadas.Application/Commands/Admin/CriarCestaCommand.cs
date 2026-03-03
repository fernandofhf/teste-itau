using ComprasProgramadas.Application.DTOs;
using ComprasProgramadas.Application.Services;
using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ComprasProgramadas.Application.Commands.Admin;

public record CriarCestaCommand(string Nome, IEnumerable<ItemCestaRequest> Itens) : IRequest<CestaResponse>;

public class CriarCestaValidator : AbstractValidator<CriarCestaCommand>
{
    public CriarCestaValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Itens).Must(itens => itens.Count() == 5)
            .WithMessage("A cesta deve conter exatamente 5 ativos.");
        RuleFor(x => x.Itens).Must(itens => Math.Abs(itens.Sum(i => i.Percentual) - 100m) < 0.01m)
            .WithMessage(x => $"A soma dos percentuais deve ser exatamente 100%. Soma atual: {x.Itens.Sum(i => i.Percentual)}%.");
        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.Ticker).NotEmpty().MaximumLength(10);
            item.RuleFor(i => i.Percentual).GreaterThan(0);
        });
    }
}

public class CriarCestaHandler : IRequestHandler<CriarCestaCommand, CestaResponse>
{
    private readonly ICestaRecomendacaoRepository _cestaRepo;
    private readonly IRebalanceamentoService _rebalanceamentoService;

    public CriarCestaHandler(ICestaRecomendacaoRepository cestaRepo, IRebalanceamentoService rebalanceamentoService)
    {
        _cestaRepo = cestaRepo;
        _rebalanceamentoService = rebalanceamentoService;
    }

    public async Task<CestaResponse> Handle(CriarCestaCommand request, CancellationToken ct)
    {
        var cestaAnterior = await _cestaRepo.ObterAtivaAsync(ct);
        List<string> ativosRemovidos = new();
        List<string> ativosAdicionados = new();
        bool rebalanceamentoDisparado = false;

        if (cestaAnterior != null)
        {
            var tickersAntigos = cestaAnterior.Itens.Select(i => i.Ticker).ToHashSet();
            var tickersNovos = request.Itens.Select(i => i.Ticker.ToUpper()).ToHashSet();

            ativosRemovidos = tickersAntigos.Except(tickersNovos).ToList();
            ativosAdicionados = tickersNovos.Except(tickersAntigos).ToList();

            cestaAnterior.Desativar();
            await _cestaRepo.AtualizarAsync(cestaAnterior, ct);
        }

        var itens = request.Itens.Select(i => new ItemCesta(i.Ticker, i.Percentual));
        var novaCesta = new CestaRecomendacao(request.Nome, itens);
        await _cestaRepo.AdicionarAsync(novaCesta, ct);

        if (cestaAnterior != null)
        {
            await _rebalanceamentoService.ExecutarRebalanceamentoPorMudancaCestaAsync(cestaAnterior, novaCesta, ct);
            rebalanceamentoDisparado = true;
        }

        var mensagem = cestaAnterior == null
            ? "Primeira cesta cadastrada com sucesso."
            : $"Cesta atualizada. Rebalanceamento disparado para os clientes ativos.";

        return new CestaResponse(
            novaCesta.Id,
            novaCesta.Nome,
            novaCesta.Ativa,
            novaCesta.DataCriacao,
            novaCesta.DataDesativacao,
            novaCesta.Itens.Select(i => new ItemCestaResponse(i.Ticker, i.Percentual)),
            rebalanceamentoDisparado,
            ativosRemovidos.Any() ? ativosRemovidos : null,
            ativosAdicionados.Any() ? ativosAdicionados : null,
            mensagem);
    }
}
