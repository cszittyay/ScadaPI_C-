using System;
using System.Collections.Generic;
using System.Linq;
using ScadaPI.CSharp; // para alias
using RegScada = ScadaPI.CSharp.PiClient.RegScada;
using RegScadaDiario = ScadaPI.CSharp.PiClient.RegScadaDiario;

namespace ScadaPI.CSharp;

internal static class UtilsScada
{
 // Detecta pares consecutivos con cambio de valor en menos de60 minutos
 public static IEnumerable<(RegScada a, RegScada b)> DetectarCambios(IEnumerable<RegScada> data)
         => data.Zip(data.Skip(1), (a, b) => (a, b))
         .Where(p => p.a.Value != p.b.Value && (p.b.FechaHora - p.a.FechaHora).TotalMinutes <60);

 // Devuelve registros donde hubo más de un cambio en el mismo día calendario
 public static IEnumerable<RegScada> DetectarMasDeUnCambio(IEnumerable<RegScada> dataSCADA)
         => DetectarCambios(dataSCADA)
         .GroupBy(pair => pair.b.FechaHora.Date)
         .Where(g => g.Count() >1)
         .SelectMany(g => g.Select(pair => new RegScada(pair.b.Tag, pair.b.FechaHora, pair.b.Value)));

 // Filtra registros quitando los días presentes en los errores
 public static IEnumerable<RegScada> FiltarMasUnCambio(IEnumerable<RegScada> dataErr, IEnumerable<RegScada> dataScada)
 {
         var fechasErr = dataErr.Select(x => x.FechaHora.Date).ToHashSet();
         return dataScada.Where(x => !fechasErr.Contains(x.FechaHora.Date));
 }

 // Detecta días con todos sus valores en cero y genera un registro diario con valor0
 public static IEnumerable<RegScadaDiario> DetectarValoresCero(IEnumerable<RegScada> dataSCADA)
            => dataSCADA
            .Select(x => new RegScadaDiario(x.Tag, DateOnly.FromDateTime(x.FechaHora.AddDays(-1)), x.Value))
            .GroupBy(x => x.DiaGas)
            .Where(g => g.All(r => Math.Abs(r.Value -0.0) < double.Epsilon))
            .Select(g => new RegScadaDiario(g.First().Tag, g.Key,0.0));
        }
