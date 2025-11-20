using System.Linq;
using Microsoft.EntityFrameworkCore;
using ScadaPI.CSharp.Data;
using static ScadaPI.CSharp.PiClient;
using static ScadaPI.CSharp.MedicionDiaria;
using RegScada = ScadaPI.CSharp.PiClient.RegScada;
using RegScadaDiario = ScadaPI.CSharp.PiClient.RegScadaDiario;

namespace ScadaPI.CSharp;

public static class InsertConsumoDiario
{

    public static void InsertMasUnCambio(ScadaDbContext ctx, TagScada tag, System.Collections.Generic.IEnumerable<RegScada> dataScada)
    {
        var aInsertar = dataScada.Where(x => x.FechaHora.AddMinutes(-1) > tag.FechaHoraError);
        foreach (var x in aInsertar)
        {
            ctx.ScadaConsumosDiariosError.Add(new ScadaConsumoDiarioError
            {
                Id_TagScada = tag.Id_Tag,
                FechaHora = x.FechaHora,
                Consumo = (decimal)x.Value
            });
        }
        if (aInsertar.Any())
        {
            var ultimoRegistro = aInsertar.MaxBy(x => x.FechaHora).FechaHora;
            ctx.TagScadas.First(x => x.Id_Tag == tag.Id_Tag).FechaHoraError = ultimoRegistro;
            ctx.SaveChanges();
        }
    }

    public static void ImportarConsumoDiario(TagScada tagScada, System.Collections.Generic.IEnumerable<RegScadaDiario> dataSCADA, ScadaDbContext ctx)
    {
        var aImportar = dataSCADA.Where(x => x.DiaGas > DateOnly.FromDateTime(tagScada.UltimaFechaHora));
        foreach (var x in aImportar)
        {
            ctx.ScadaConsumosDiarios.Add(new ScadaConsumoDiario
            {
                Id_TagScada = tagScada.Id_Tag,
                DiaGas = x.DiaGas.ToDateTime(new TimeOnly(0,0)),
                Consumo = (decimal)x.Value
            });
        }
        if (aImportar.Any())
        {
            var ultimoRegistro = aImportar.MaxBy(x => x.DiaGas).DiaGas.ToDateTime(new TimeOnly(0, 0));
            ctx.TagScadas.First(x => x.Id_Tag == tagScada.Id_Tag).UltimaFechaHora = ultimoRegistro;
            ctx.SaveChanges();
        }
    }

    public static void ImportarConsumoDiarioPorTag(string piConnection)
    {
        var options = new DbContextOptionsBuilder<ScadaDbContext>()
            .UseSqlServer(ScadaDbContext.DefaultConnection)
            .Options;

        using var ctx = new ScadaDbContext(options);

        var tags = ctx.TagScadas.AsNoTracking().Where(t => t.Importar && t.Unidad == "M3_STD" && t.Frecuencia == "D").ToList();

        foreach (var tag in tags)
        {
            Console.WriteLine($"Importando consumos para el tag: {tag.Tag}");

            var dataSCADA = GetPi(piConnection, tag.Tag, tag.UltimaFechaHora);
            var masDeUnCambio = UtilsScada.DetectarMasDeUnCambio(dataSCADA).ToList();
            InsertMasUnCambio(ctx, tag, masDeUnCambio);

            var okDataScada = UtilsScada.FiltarMasUnCambio(masDeUnCambio, dataSCADA).ToList();

            var consumoPositivo = GetMedicionDiaria(dataSCADA).ToList();
            var diasConsumoPositivo = consumoPositivo.Select(x => x.DiaGas).ToHashSet();
            var consumoCero = UtilsScada.DetectarValoresCero(okDataScada).Where(x => !diasConsumoPositivo.Contains(x.DiaGas)).ToList();

            var consumos = consumoPositivo.Concat(consumoCero).ToList();

            ImportarConsumoDiario(tag, consumos, ctx);
        }
    }
}
