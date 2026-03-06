using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Interfaces;
using ComprasProgramadas.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.Infrastructure.Persistence.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context) => _context = context;

    public async Task<Cliente?> ObterPorIdAsync(long id, CancellationToken ct = default)
        => await _context.Clientes.Include(c => c.ContaGrafica).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Cliente?> ObterPorCpfAsync(string cpf, CancellationToken ct = default)
        => await _context.Clientes.FirstOrDefaultAsync(c => c.CPF == cpf, ct);

    public async Task<IEnumerable<Cliente>> ObterAtivosAsync(CancellationToken ct = default)
        => await _context.Clientes.Where(c => c.Ativo).ToListAsync(ct);

    public async Task AdicionarAsync(Cliente cliente, CancellationToken ct = default)
    {
        await _context.Clientes.AddAsync(cliente, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AtualizarAsync(Cliente cliente, CancellationToken ct = default)
    {
        _context.Clientes.Update(cliente);
        await _context.SaveChangesAsync(ct);
    }

}
