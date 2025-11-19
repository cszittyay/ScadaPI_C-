using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ScadaPI.CSharp.Data;
using static ScadaPI.CSharp.PiClient;
using static ScadaPI.CSharp.MedicionDiaria;

namespace ScadaPI.CSharp;

public static class InsertPoderCalorificoDiario
{

    private static System.Collections.Generic.IEnumerable<(RegScada a, RegScada b)> DetectarCambios(System.Collections.Generic.IEnumerable<RegScada> data)
        => data.Zip(data.Skip(1), (a, b) => (a, b)).Where(p => p.a.Value != p.b.Value && (p.a.FechaHora - p.a.FechaHora).TotalMinutes < 60);

   
    public static void InsertMasUnCambio(ScadaDbContext ctx, string tag, System.Collections.Generic.IEnumerable<RegScada> dataScada)
    {
        var rTag = ctx.TagScadas.Single(t => t.Tag == tag);
        var aInsertar = dataScada.Where(x => x.FechaHora.AddMinutes(-1) > rTag.FechaHoraError);
        foreach (var x in aInsertar)
        {
            ctx.ScadaPoderCalorificosDiariosError.Add(new ScadaPoderCalorificoDiarioError
            {
                Id_TagScada = rTag.Id_Tag,
                FechaHora = x.FechaHora,
                PoderCalorifico = (decimal)x.Value
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

    public static System.Collections.Generic.IEnumerable<RegScadaDiario> DetectarPoderCarlorificioCero(System.Collections.Generic.IEnumerable<RegScada> dataSCADA)
        => dataSCADA
            .Select(x => new RegScadaDiario(x.Tag, DateOnly.FromDateTime(x.FechaHora.AddDays(-1)), x.Value))
            .GroupBy(x => x.DiaGas)
            .Where(g => g.All(r => Math.Abs(r.Value - 0.0) < double.Epsilon))
            .Select(g => new RegScadaDiario(g.First().Tag, g.Key, 0.0));

    public static void ImportarPoderCalorificoDiario(TagScada tagScada, System.Collections.Generic.IEnumerable<RegScadaDiario> dataSCADA, ScadaDbContext ctx)
    {
        var ultimaFecha = tagScada.UltimaFechaHora;
        var aImportar = dataSCADA.Where(x => x.DiaGas > DateOnly.FromDateTime(ultimaFecha));
        foreach (var x in aImportar)
        {
            ctx.ScadaPoderCalorificosDiarios.Add(new ScadaPoderCalorificoDiario
            {
                Id_TagScada = tagScada.Id_Tag,
                DiaGas = x.DiaGas.ToDateTime(new TimeOnly(0, 0)),
                PoderCalorifico = (decimal)x.Value
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

    public static void ImportarPoderCalorificoDiarioPorTag(string piConnection)
    {
        var options = new DbContextOptionsBuilder<ScadaDbContext>()
            .UseSqlServer(ScadaDbContext.DefaultConnection)
            .Options;

        using var ctx = new ScadaDbContext(options);

        var tags = ctx.TagScadas.AsNoTracking().Where(t => t.Importar && t.Unidad == "MJ/m3" && t.Frecuencia == "D").Select(t => t.Tag).ToList();

        foreach (var tag in tags)
        {
            Console.WriteLine($"Importando PoderCarlorificios para el tag: {tag}");

            var diasHistoria = 365;
            var dataSCADA = GetPi(piConnection, tag, diasHistoria);
            var masDeUnCambio = DetectarMasDeUnCambio(dataSCADA).ToList();
            InsertMasUnCambio(ctx, tag, masDeUnCambio);

            var okDataScada = FiltarMasUnCambio(masDeUnCambio, dataSCADA).ToList();

            var PoderCarlorificioPositivo = GetMedicionDiaria(dataSCADA).ToList();
            var diasPoderCarlorificioPositivo = PoderCarlorificioPositivo.Select(x => x.DiaGas).ToHashSet();
            var PoderCarlorificioCero = DetectarPoderCarlorificioCero(okDataScada).Where(x => !diasPoderCarlorificioPositivo.Contains(x.DiaGas)).ToList();

            var PoderCarlorificios = PoderCarlorificioPositivo.Concat(PoderCarlorificioCero).ToList();

            var tagInfo = ctx.TagScadas.Single(t => t.Tag == tag);
            ImportarPoderCalorificoDiario(tagInfo, PoderCarlorificios, ctx);
        }
    }
}
