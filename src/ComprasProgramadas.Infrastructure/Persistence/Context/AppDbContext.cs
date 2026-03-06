using ComprasProgramadas.Domain.Entities;
using ComprasProgramadas.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ComprasProgramadas.Infrastructure.Persistence.Context;

public class AppDbContext : DbContext
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ContaGrafica> ContasGraficas => Set<ContaGrafica>();
    public DbSet<Custodia> Custodias => Set<Custodia>();
    public DbSet<CestaRecomendacao> CestasRecomendacao => Set<CestaRecomendacao>();
    public DbSet<ItemCesta> ItensCesta => Set<ItemCesta>();
    public DbSet<OrdemCompra> OrdensCompra => Set<OrdemCompra>();
    public DbSet<Distribuicao> Distribuicoes => Set<Distribuicao>();
    public DbSet<EventoIR> EventosIR => Set<EventoIR>();
    public DbSet<Cotacao> Cotacoes => Set<Cotacao>();
    public DbSet<Rebalanceamento> Rebalanceamentos => Set<Rebalanceamento>();
    public DbSet<HistoricoAporte> HistoricoAportes => Set<HistoricoAporte>();
    public DbSet<HistoricoOrdemCliente> HistoricoOrdensCliente => Set<HistoricoOrdemCliente>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cliente
        modelBuilder.Entity<Cliente>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedOnAdd();
            e.Property(c => c.Nome).HasMaxLength(200).IsRequired();
            e.Property(c => c.CPF).HasMaxLength(11).IsRequired();
            e.HasIndex(c => c.CPF).IsUnique();
            e.Property(c => c.Email).HasMaxLength(200).IsRequired();
            e.Property(c => c.ValorMensal).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(c => c.Ativo).HasDefaultValue(true);
            e.HasOne(c => c.ContaGrafica)
                .WithOne(cg => cg.Cliente)
                .HasForeignKey<ContaGrafica>(cg => cg.ClienteId)
                .IsRequired(false);
        });

        // ContaGrafica
        modelBuilder.Entity<ContaGrafica>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedOnAdd();
            e.Property(c => c.NumeroConta).HasMaxLength(20).IsRequired();
            e.HasIndex(c => c.NumeroConta).IsUnique();
            e.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(10);
            e.HasMany(c => c.Custodias)
                .WithOne(cu => cu.ContaGrafica)
                .HasForeignKey(cu => cu.ContaGraficaId);
        });

        // Custodia
        modelBuilder.Entity<Custodia>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedOnAdd();
            e.Property(c => c.Ticker).HasMaxLength(10).IsRequired();
            e.Property(c => c.PrecoMedio).HasColumnType("decimal(18,4)");
            e.HasIndex(c => new { c.ContaGraficaId, c.Ticker }).IsUnique();
        });

        // CestaRecomendacao
        modelBuilder.Entity<CestaRecomendacao>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedOnAdd();
            e.Property(c => c.Nome).HasMaxLength(100).IsRequired();
            e.HasMany(c => c.Itens)
                .WithOne(i => i.Cesta)
                .HasForeignKey(i => i.CestaId);
        });

        // ItemCesta
        modelBuilder.Entity<ItemCesta>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).ValueGeneratedOnAdd();
            e.Property(i => i.Ticker).HasMaxLength(10).IsRequired();
            e.Property(i => i.Percentual).HasColumnType("decimal(5,2)");
        });

        // OrdemCompra
        modelBuilder.Entity<OrdemCompra>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Id).ValueGeneratedOnAdd();
            e.Property(o => o.Ticker).HasMaxLength(10).IsRequired();
            e.Property(o => o.PrecoUnitario).HasColumnType("decimal(18,4)");
            e.Property(o => o.TipoMercado).HasConversion<string>().HasMaxLength(15);
            e.Ignore(o => o.ValorTotal);
            e.HasMany(o => o.Distribuicoes)
                .WithOne(d => d.OrdemCompra)
                .HasForeignKey(d => d.OrdemCompraId);
        });

        // Distribuicao
        modelBuilder.Entity<Distribuicao>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).ValueGeneratedOnAdd();
            e.Property(d => d.Ticker).HasMaxLength(10).IsRequired();
            e.Property(d => d.PrecoUnitario).HasColumnType("decimal(18,4)");
            e.Ignore(d => d.ValorOperacao);
        });

        // EventoIR
        modelBuilder.Entity<EventoIR>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.Id).ValueGeneratedOnAdd();
            e.Property(ev => ev.Tipo).HasConversion<string>().HasMaxLength(15);
            e.Property(ev => ev.ValorBase).HasColumnType("decimal(18,2)");
            e.Property(ev => ev.ValorIR).HasColumnType("decimal(18,2)");
        });

        // Cotacao
        modelBuilder.Entity<Cotacao>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedOnAdd();
            e.Property(c => c.Ticker).HasMaxLength(10).IsRequired();
            e.Property(c => c.PrecoAbertura).HasColumnType("decimal(18,4)");
            e.Property(c => c.PrecoFechamento).HasColumnType("decimal(18,4)");
            e.Property(c => c.PrecoMaximo).HasColumnType("decimal(18,4)");
            e.Property(c => c.PrecoMinimo).HasColumnType("decimal(18,4)");
            e.HasIndex(c => new { c.Ticker, c.DataPregao }).IsUnique();
        });

        // Rebalanceamento
        modelBuilder.Entity<Rebalanceamento>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).ValueGeneratedOnAdd();
            e.Property(r => r.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.TickerVendido).HasMaxLength(10);
            e.Property(r => r.TickerComprado).HasMaxLength(10);
            e.Property(r => r.ValorVenda).HasColumnType("decimal(18,2)");
        });

        // HistoricoAporte
        modelBuilder.Entity<HistoricoAporte>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).ValueGeneratedOnAdd();
            e.Property(h => h.ValorAnterior).HasColumnType("decimal(18,2)");
            e.Property(h => h.ValorNovo).HasColumnType("decimal(18,2)");
            e.HasOne(h => h.Cliente)
                .WithMany()
                .HasForeignKey(h => h.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // HistoricoOrdemCliente
        modelBuilder.Entity<HistoricoOrdemCliente>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Id).ValueGeneratedOnAdd();
            e.Property(h => h.Ticker).HasMaxLength(10).IsRequired();
            e.Property(h => h.TipoOrdem).HasConversion<string>().HasMaxLength(10);
            e.Property(h => h.PrecoUnitario).HasColumnType("decimal(18,4)");
            e.Property(h => h.ValorTotal).HasColumnType("decimal(18,2)");
            e.Property(h => h.Origem).HasConversion<string>().HasMaxLength(30);
            e.HasOne(h => h.Cliente)
                .WithMany()
                .HasForeignKey(h => h.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
