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

    public static void ImportarPoderCalorificoDiario(TagScada tagScada, System.Collections.Generic.IEnumerable<RegScadaDiario> dataSCADA, ScadaDbContext ctx)
    {
         foreach (var x in dataSCADA)
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
            ctx.SaveChanges();
            var ultimoRegistro = dataSCADA.MaxBy(x => x.DiaGas);
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

        var tags = ctx.TagScadas.AsNoTracking().Where(t => t.Importar && t.Unidad == "MJ/m3" && t.Frecuencia == "D").ToList();

        foreach (var tag in tags)
        {
            Console.WriteLine($"Importando PoderCarlorificios para el tag: {tag}");

            var dataSCADA = GetPi(piConnection, tag.Tag, tag.UltimaFechaHora);
            var masDeUnCambio = UtilsScada.DetectarMasDeUnCambio(dataSCADA).ToList();
            InsertMasUnCambio(ctx, tag.Tag, masDeUnCambio);

            var okDataScada = UtilsScada.FiltarMasUnCambio(masDeUnCambio, dataSCADA).ToList();

            var poderCalorificoPositivo = GetMedicionDiaria(dataSCADA).ToList();
            var diasPoderCalorificoPositivo = poderCalorificoPositivo.Select(x => x.DiaGas).ToHashSet();
            var poderCalorificoCero = UtilsScada.DetectarValoresCero(okDataScada).Where(x => !diasPoderCalorificoPositivo.Contains(x.DiaGas)).ToList();

            var poderCalorificos = poderCalorificoPositivo.Concat(poderCalorificoCero).ToList();

            ImportarPoderCalorificoDiario(tag, poderCalorificos, ctx);
        }
    }
}
