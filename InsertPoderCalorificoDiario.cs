using System.Linq;
using Microsoft.EntityFrameworkCore;
using ScadaPI.CSharp.Data;
using static ScadaPI.CSharp.PiClient;
using static ScadaPI.CSharp.MedicionDiaria;
using RegScada = ScadaPI.CSharp.PiClient.RegScada;
using RegScadaDiario = ScadaPI.CSharp.PiClient.RegScadaDiario;

namespace ScadaPI.CSharp;

public static class InsertPoderCalorificoDiario
{
    public static void InsertMasUnCambio(ScadaDbContext ctx, TagScada tag, IEnumerable<RegScada> dataScada)
    {
        var aInsertar = dataScada.Where(x => x.FechaHora.AddMinutes(-1) > tag.FechaHoraError);
        foreach (var x in aInsertar)
        {
            ctx.ScadaPoderCalorificosDiariosError.Add(new ScadaPoderCalorificoDiarioError
            {
                Id_TagScada = tag.Id_Tag,
                FechaHora = x.FechaHora,
                PoderCalorifico = (decimal)x.Value
            });
        }
        if (aInsertar.Any())
        {
            ctx.TagScadas.First(t => t.Id_Tag == tag.Id_Tag).FechaHoraError = aInsertar.MaxBy(x => x.FechaHora).FechaHora;
            ctx.SaveChanges();
        }
    }  

    public static void ImportarPoderCalorificoDiario(TagScada tagScada, IEnumerable<RegScadaDiario> dataSCADA, ScadaDbContext ctx)
    {
        
        foreach (var x in dataSCADA.Where(d => d.DiaGas.ToDateTime(TimeOnly.MinValue) > tagScada.UltimaFechaHora))
        {
            ctx.ScadaPoderCalorificosDiarios.Add(new ScadaPoderCalorificoDiario
            {
                Id_TagScada = tagScada.Id_Tag,
                DiaGas = x.DiaGas.ToDateTime(new TimeOnly(0, 0)),
                PoderCalorifico = (decimal)x.Value
            });
        }
        if (dataSCADA.Any())
        {
            var ultimaFecha = dataSCADA.MaxBy(x => x.DiaGas).DiaGas.ToDateTime(TimeOnly.MinValue);
            ctx.TagScadas.First(t => t.Id_Tag == tagScada.Id_Tag).UltimaFechaHora = ultimaFecha;
            ctx.SaveChanges();
        }
    }

    public static void ImportarPoderCalorificoDiarioPorTag(string piConnection)
    {
        var options = new DbContextOptionsBuilder<ScadaDbContext>()
            .UseSqlServer(ScadaDbContext.DefaultConnection)
            .Options;

        using var ctx = new ScadaDbContext(options);

        var tags = ctx.TagScadas.AsNoTracking().Where(t => t.Importar && t.Unidad == "MJ/m3" && t.Frecuencia == "D").ToList();

        foreach (var tag in tags)
        {
            Console.WriteLine($"Importando PoderCarlorificios para el tag: {tag.Tag}");

            var dataSCADA = GetPi(piConnection, tag.Tag, tag.UltimaFechaHora);
            var masDeUnCambio = UtilsScada.DetectarMasDeUnCambio(dataSCADA).ToList();
            InsertMasUnCambio(ctx, tag, masDeUnCambio);

            var okDataScada = UtilsScada.FiltarMasUnCambio(masDeUnCambio, dataSCADA).ToList();

            var poderCalorificoPositivo = GetMedicionDiaria(dataSCADA).ToList();
            var diasPoderCalorificoPositivo = poderCalorificoPositivo.Select(x => x.DiaGas).ToHashSet();
            var poderCalorificoCero = UtilsScada.DetectarValoresCero(okDataScada).Where(x => !diasPoderCalorificoPositivo.Contains(x.DiaGas)).ToList();

            var poderCalorificos = poderCalorificoPositivo.Concat(poderCalorificoCero).ToList();

            ImportarPoderCalorificoDiario(tag, poderCalorificos, ctx);
        }
    }
}
