using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ScadaPI.CSharp.Data;
using static ScadaPI.CSharp.PiClient;
using static ScadaPI.CSharp.MedicionDiaria;

namespace ScadaPI.CSharp;

public static class InsertConsumoDiario
{

    private static System.Collections.Generic.IEnumerable<(RegScada a, RegScada b)> DetectarCambios(System.Collections.Generic.IEnumerable<RegScada> data)
        => data.Zip(data.Skip(1), (a, b) => (a, b)).Where(p => p.a.Value != p.b.Value && (p.a.FechaHora - p.a.FechaHora).TotalMinutes < 60);

   
    public static void InsertMasUnCambio(ScadaDbContext ctx, string tag, System.Collections.Generic.IEnumerable<RegScada> dataScada)
    {
        var rTag = ctx.TagScadas.Single(t => t.Tag == tag);
        var aInsertar = dataScada.Where(x => x.FechaHora.AddMinutes(-1) > rTag.FechaHoraError);
        foreach (var x in aInsertar)
        {
            ctx.ScadaConsumosDiariosError.Add(new ScadaConsumoDiarioError
            {
                Id_TagScada = rTag.Id_Tag,
                FechaHora = x.FechaHora,
                Consumo = (decimal)x.Value
            });
        }
        if (aInsertar.Any())
        {
            rTag.FechaHoraError = aInsertar.Max(x => x.FechaHora);
            ctx.SaveChanges();
        }
    }

    public static System.Collections.Generic.IEnumerable<RegScada> DetectarMasDeUnCambio(System.Collections.Generic.IEnumerable<RegScada> dataSCADA)
        => DetectarCambios(dataSCADA)
            .GroupBy(pair => pair.b.FechaHora.Date)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Select(pair => new RegScada(pair.b.Tag, pair.b.FechaHora, pair.b.Value)));

    public static System.Collections.Generic.IEnumerable<RegScada> FiltarMasUnCambio(System.Collections.Generic.IEnumerable<RegScada> dataErr, System.Collections.Generic.IEnumerable<RegScada> dataScada)
    {
        var fechasErr = dataErr.Select(x => x.FechaHora.Date).ToHashSet();
        return dataScada.Where(x => !fechasErr.Contains(x.FechaHora.Date));
    }

    public static System.Collections.Generic.IEnumerable<RegScadaDiario> DetectarConsumoCero(System.Collections.Generic.IEnumerable<RegScada> dataSCADA)
        => dataSCADA
            .Select(x => new RegScadaDiario(x.Tag, DateOnly.FromDateTime(x.FechaHora.AddDays(-1)), x.Value))
            .GroupBy(x => x.DiaGas)
            .Where(g => g.All(r => Math.Abs(r.Value - 0.0) < double.Epsilon))
            .Select(g => new RegScadaDiario(g.First().Tag, g.Key, 0.0));

    public static void ImportarConsumoDiario(TagScada tagScada, System.Collections.Generic.IEnumerable<RegScadaDiario> dataSCADA, ScadaDbContext ctx)
    {
        var ultimaFecha = tagScada.UltimaFechaHora;
        var aImportar = dataSCADA.Where(x => x.DiaGas > DateOnly.FromDateTime(ultimaFecha));
        foreach (var x in aImportar)
        {
            ctx.ScadaConsumosDiarios.Add(new ScadaConsumoDiario
            {
                Id_TagScada = tagScada.Id_Tag,
                DiaGas = x.DiaGas.ToDateTime(new TimeOnly(0, 0)),
                Consumo = (decimal)x.Value
            });
        }
        if (aImportar.Any())
        {
            ctx.SaveChanges();
            var ultimoRegistro = aImportar.MaxBy(x => x.DiaGas);
            tagScada.UltimaFechaHora = ultimoRegistro.DiaGas.ToDateTime(new TimeOnly(0, 0));
            ctx.SaveChanges();
        }
    }

    public static void ImportarConsumoDiarioPorTag(string piConnection)
    {
        var options = new DbContextOptionsBuilder<ScadaDbContext>()
            .UseSqlServer(ScadaDbContext.DefaultConnection)
            .Options;

        using var ctx = new ScadaDbContext(options);

        var tags = ctx.TagScadas.AsNoTracking().Where(t => t.Importar && t.Unidad == "M3_STD" && t.Frecuencia == "D").Select(t => t.Tag ).ToList();

        foreach (var tag in tags)
        {
            Console.WriteLine($"Importando consumos para el tag: {tag}");

            var diasHistoria = 365;
            var dataSCADA = GetPi(piConnection, tag, diasHistoria);
            var masDeUnCambio = DetectarMasDeUnCambio(dataSCADA).ToList();
            InsertMasUnCambio(ctx, tag, masDeUnCambio);

            var okDataScada = FiltarMasUnCambio(masDeUnCambio, dataSCADA).ToList();

            var consumoPositivo = GetMedicionDiaria(dataSCADA).ToList();
            var diasConsumoPositivo = consumoPositivo.Select(x => x.DiaGas).ToHashSet();
            var consumoCero = DetectarConsumoCero(okDataScada).Where(x => !diasConsumoPositivo.Contains(x.DiaGas)).ToList();

            var consumos = consumoPositivo.Concat(consumoCero).ToList();

            var tagInfo = ctx.TagScadas.Single(t => t.Tag == tag);
            ImportarConsumoDiario(tagInfo, consumos, ctx);
        }
    }
}
