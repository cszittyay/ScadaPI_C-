using System.Linq;
using Microsoft.EntityFrameworkCore;
using ScadaPI.CSharp.Data;
using static ScadaPI.CSharp.PiClient;
using static ScadaPI.CSharp.MedicionDiaria;

namespace ScadaPI.CSharp;

public static class InsertPoderCalorificoHorario
{

    // Detectar PoderCalorifico cero
    // Value = 0 y tomar valores con diferencia de 1 hora
    public static IEnumerable<RegScada> GetPoderCalorificoCero(IEnumerable<RegScada> dataSCADA)
    {
        var PoderCalorificoCero = dataSCADA.Where(x => x.Value == 0.0).OrderBy(x => x.FechaHora);
        var minutosMinimos = 55;
        if (!PoderCalorificoCero.Any())
        {
            yield break;
        }
        DateTime lastTaken = PoderCalorificoCero.First().FechaHora;
        foreach (var r in PoderCalorificoCero)
        {
            if ((r.FechaHora - lastTaken).TotalMinutes >= minutosMinimos)
            {
                yield return r;
                lastTaken = r.FechaHora;
            }
        }
        
    }

    public static void ImportarPoderCalorificoHorario(TagScada tagScada, IEnumerable<RegScada> dataSCADA, ScadaDbContext ctx)
    {
        if(!dataSCADA.Any())
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
            ctx.ScadaPoderCalorificosHorarios.Add(new ScadaPoderCalorificoHorario
            {
                Id_TagScada = tagScada.Id_Tag,
                FechaHora = x.FechaHora,
                PoderCalorifico = (decimal)x.Value
            });
        }
        if (dataSCADAList.Any())
        {
            var ultimaFechaHora = dataSCADAList.MaxBy(x => x.FechaHora).FechaHora;
            ctx.TagScadas.First(t => t.Id_Tag == tagScada.Id_Tag).UltimaFechaHora = ultimaFechaHora;
            ctx.SaveChanges();
        }
    }

    public static void ImportarPoderCalorificoHorarioPorTag(string piConnection)
    {
        var options = new DbContextOptionsBuilder<ScadaDbContext>()
            .UseSqlServer(ScadaDbContext.DefaultConnection)
            .Options;

        using var ctx = new ScadaDbContext(options);

        var tags = ctx.TagScadas.AsNoTracking().Where(t => t.Importar && t.Unidad == "MJ/m3" && t.Frecuencia == "H").ToList();

        foreach (var tag in tags)
        {
            Console.WriteLine($"Importando PoderCalorifico Horario para el tag: {tag.Tag}");

            var dataSCADA = GetPi(piConnection, tag.Tag, tag.UltimaFechaHora);

            var PoderCalorificoCero = GetPoderCalorificoCero(dataSCADA).ToList();

            var PoderCalorificosPositivo = MedicionHoraria.GetMedicionHoraria(dataSCADA).ToList();
            // quitar los PoderCalorificos positivos que ya esten en PoderCalorifico cero
            PoderCalorificosPositivo = PoderCalorificosPositivo.Where(x => !PoderCalorificoCero.Any(c => c.FechaHora == x.FechaHora)).ToList();

            var PoderCalorificos = PoderCalorificoCero.Concat(PoderCalorificosPositivo);

            ImportarPoderCalorificoHorario(tag, PoderCalorificos, ctx);
        }
    }
}
