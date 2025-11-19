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
    // Consumo Diario
    public DbSet<ScadaConsumoDiario> ScadaConsumosDiarios => Set<ScadaConsumoDiario>();
    public DbSet<ScadaConsumoDiarioError> ScadaConsumosDiariosError => Set<ScadaConsumoDiarioError>();

    // Poder Calorífico diario
    public DbSet<ScadaPoderCalorificoDiario> ScadaPoderCalorificosDiarios => Set<ScadaPoderCalorificoDiario>();
    public DbSet<ScadaPoderCalorificoDiarioError> ScadaPoderCalorificosDiariosError => Set<ScadaPoderCalorificoDiarioError>();
    
    
    // Datos horarios
    public DbSet<ScadaConsumoHorario> ScadaConsumosHorarios => Set<ScadaConsumoHorario>();

    public DbSet<ScadaPoderCalorificoHorario> ScadaPoderCalorificosHorarios => Set<ScadaPoderCalorificoHorario>();

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
            e.Property(x => x.UltimaFechaHora).IsRequired();
            e.Property(x => x.FechaHoraError).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(100);
            e.Property(x => x.UltimoValor).HasColumnType("decimal(18,6)").IsRequired();
        });

        modelBuilder.Entity<ScadaConsumoDiario>(e =>
        {
            e.ToTable("Scada_ConsumoDiario");
            e.HasKey(x => new { x.Id_TagScada, x.DiaGas });
            e.Property(x => x.Id_TagScada).IsRequired();
            e.Property(x => x.DiaGas).IsRequired();
            e.Property(x => x.Consumo).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.Tag)
             .WithMany()
             .HasForeignKey(x => x.Id_TagScada);
        });


        modelBuilder.Entity<ScadaConsumoDiarioError>(e =>
        {
            e.ToTable("Scada_ConsumoDiarioError");
            e.HasKey(x => new { x.Id_TagScada, x.FechaHora });
            e.Property(x => x.Id_TagScada).IsRequired();
            e.Property(x => x.FechaHora).IsRequired(); 
            e.Property(x => x.Consumo).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.Tag)
             .WithMany()
             .HasForeignKey(x => x.Id_TagScada);
        });


        modelBuilder.Entity<ScadaPoderCalorificoDiario>(e =>
        {
            e.ToTable("Scada_PoderCalorificoDiario");
            e.HasKey(x => new { x.Id_TagScada, x.DiaGas });
            e.Property(x => x.Id_TagScada).IsRequired();
            e.Property(x => x.DiaGas).IsRequired();
            e.Property(x => x.PoderCalorifico).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.Tag)
             .WithMany()
             .HasForeignKey(x => x.Id_TagScada);
        });

        modelBuilder.Entity<ScadaPoderCalorificoDiarioError>(e =>
        {
            e.ToTable("Scada_PoderCalorificoDiarioError");
            e.HasKey(x => new { x.Id_TagScada, x.FechaHora });
            e.Property(x => x.Id_TagScada).IsRequired();
            e.Property(x => x.FechaHora).IsRequired();
            e.Property(x => x.PoderCalorifico).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.Tag)
             .WithMany()
             .HasForeignKey(x => x.Id_TagScada);
        });



        // Datos Horarios
        modelBuilder.Entity<ScadaConsumoHorario>(e =>
        {
            e.ToTable("Scada_ConsumoHorario");
            e.HasKey(x => new { x.Id_TagScada, x.FechaHora });
            e.Property(x => x.FechaHora).IsRequired();
            e.Property(x => x.Id_TagScada).IsRequired();
            e.Property(x => x.Consumo).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.Tag)
             .WithMany()
             .HasForeignKey(x => x.Id_TagScada);
        });
        
        modelBuilder.Entity<ScadaPoderCalorificoHorario>(e =>
        {
            e.ToTable("Scada_PoderCalorificoHorario");
            e.HasKey(x => new { x.Id_TagScada, x.FechaHora });
            e.Property(x => x.Id_TagScada).IsRequired();
            e.Property(x => x.FechaHora).IsRequired();
            e.Property(x => x.PoderCalorifico).HasColumnType("decimal(18,4)");
            e.HasOne(x => x.Tag)
             .WithMany()
             .HasForeignKey(x => x.Id_TagScada);
        });

    }
}
