using System.Linq;
using Microsoft.EntityFrameworkCore;
using ScadaPI.CSharp.Data;
using static ScadaPI.CSharp.PiClient;
using static ScadaPI.CSharp.MedicionDiaria;
using RegScada = ScadaPI.CSharp.PiClient.RegScada;

namespace ScadaPI.CSharp;

public static class InsertConsumoHorario
{

    // Detectar consumo cero
    // Value = 0 y tomar valores con diferencia de 1 hora
    public static IEnumerable<RegScada> GetConsumoCero(IEnumerable<RegScada> dataSCADA)
    {
        var consumoCero = dataSCADA.Where(x => x.Value == 0.0).OrderBy(x => x.FechaHora);
        var minutosMinimos = 55;
        if (!consumoCero.Any())
        {
            yield break;
        }
        DateTime lastTaken = consumoCero.First().FechaHora;
        foreach (var r in consumoCero)
        {
            if ((r.FechaHora - lastTaken).TotalMinutes >= minutosMinimos)
            {
                yield return r;
                lastTaken = r.FechaHora;
            }
        }
        
    }

    public static void ImportarConsumoHorario(TagScada tagScada, IEnumerable<RegScada> dataSCADA, ScadaDbContext ctx)
    {
        if (!dataSCADA.Any())
        {
            return;
        }
        // pasar los datos a una lista en memoria para poder redondear la FechaHora a 5 minutos
        var dataSCADAList = dataSCADA.
                            ToList().
                            OrderBy(x => x.FechaHora).
                            Select(x => new RegScada(x.Tag, UtilsScada.RoundToXMinutes(x.FechaHora), x.Value)).
                            GroupBy(x => x.FechaHora).
                            Select(x => x.First()).
                            ToList();

        foreach (var x in dataSCADAList)
        {
            ctx.ScadaConsumosHorarios.Add(new ScadaConsumoHorario
            {
                Id_TagScada = tagScada.Id_Tag,
                FechaHora = x.FechaHora,
                Consumo = (decimal)x.Value
            });
        }
        if (dataSCADAList.Any())
        {
            var ultimaLectura = dataSCADAList.MaxBy(x => x.FechaHora).FechaHora;
            ctx.TagScadas.First(t => t.Id_Tag == tagScada.Id_Tag).UltimaFechaHora = ultimaLectura;
            ctx.SaveChanges();
        }
    }

    public static void ImportarConsumoHorarioPorTag(string piConnection)
    {
        var options = new DbContextOptionsBuilder<ScadaDbContext>()
            .UseSqlServer(ScadaDbContext.DefaultConnection)
            .Options;

        using var ctx = new ScadaDbContext(options);

        var tags = ctx.TagScadas.AsNoTracking().Where(t => t.Importar && t.Unidad == "STD_m3 / h" && t.Frecuencia == "H").ToList();

        foreach (var tag in tags)
        {
            Console.WriteLine($"Importando consumos Horario para el tag: {tag.Tag}");

            var dataSCADA = GetPi(piConnection, tag.Tag, tag.UltimaFechaHora);

            var consumoCero = GetConsumoCero(dataSCADA).ToList();

            var consumosPositivo = MedicionHoraria.GetMedicionHoraria(dataSCADA).ToList();
            // quitar los consumos positivos que ya esten en consumo cero
            consumosPositivo = consumosPositivo.Where(x => !consumoCero.Any(c => c.FechaHora == x.FechaHora)).ToList();

            var consumos = consumoCero.Concat(consumosPositivo);

            ImportarConsumoHorario(tag, consumos, ctx);
        }
    }
}
