using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace ScadaPI.CSharp.Data;

public class ScadaDbContext : DbContext
{
    public const string DefaultConnection = "Server=DESKTOP-8GOI1HK;Database=GNX.core;Trusted_Connection=True;TrustServerCertificate=True;";

    public ScadaDbContext(DbContextOptions<ScadaDbContext> options) : base(options) { }

    public DbSet<TagScada> TagScadas => Set<TagScada>();
    public DbSet<ScadaConsumoDiario> ScadaConsumosDiarios => Set<ScadaConsumoDiario>();
    public DbSet<ScadaConsumoDiarioError> ScadaConsumosDiariosError => Set<ScadaConsumoDiarioError>();


    public DbSet<ScadaPoderCalorificoDiario> ScadaPoderCalorificosDiarios => Set<ScadaPoderCalorificoDiario>();
    public DbSet<ScadaPoderCalorificoDiarioError> ScadaPoderCalorificosDiariosError => Set<ScadaPoderCalorificoDiarioError>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("saavi");

        modelBuilder.Entity<TagScada>(e =>
        {
            e.ToTable("TagSCADA");
            e.HasKey(x => x.Id_Tag);
            e.Property(x => x.Tag).HasMaxLength(200).IsRequired();
            e.Property(x => x.Frecuencia).HasMaxLength(50).IsRequired();
            e.Property(x => x.Unidad).HasMaxLength(50).IsRequired();
            e.Property(x => x.FactorAjuste).HasColumnType("decimal(18,6)").IsRequired();
            e.Property(x => x.Importar).IsRequired();
        });

        modelBuilder.Entity<ScadaConsumoDiario>(e =>
        {
            e.ToTable("Scada_ConsumoDiario");
            e.HasKey(x => new { x.Id_TagScada, x.DiaGas });
            e.Property(x => x.Consumo).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.Tag)
             .WithMany()
             .HasForeignKey(x => x.Id_TagScada);
        });

        modelBuilder.Entity<ScadaConsumoDiarioError>(e =>
        {
            e.ToTable("Scada_ConsumoDiarioError");
            e.HasKey(x => new { x.Id_TagScada, x.FechaHora });
            e.Property(x => x.Consumo).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.Tag)
             .WithMany()
             .HasForeignKey(x => x.Id_TagScada);
        });

        modelBuilder.Entity<ScadaPoderCalorificoDiario>(e =>
        {
            e.ToTable("Scada_PoderCalorificoDiario");
            e.HasKey(x => new { x.Id_TagScada, x.DiaGas });
            e.Property(x => x.PoderCalorifico).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.Tag)
             .WithMany()
             .HasForeignKey(x => x.Id_TagScada);
        });

        modelBuilder.Entity<ScadaPoderCalorificoDiarioError>(e =>
        {
            e.ToTable("Scada_PoderCalorificoDiarioError");
            e.HasKey(x => new { x.Id_TagScada, x.FechaHora });
            e.Property(x => x.PoderCalorifico).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.Tag)
             .WithMany()
             .HasForeignKey(x => x.Id_TagScada);
        });


    }
}
