using System;
using System.Collections.Generic;
using System.Linq;
using static ScadaPI.CSharp.DbContextPI;
using static ScadaPI.CSharp.PiClient;

namespace ScadaPI.CSharp;

public static class MedicionDiaria
{
    private static IEnumerable<(RegScada a, RegScada b)> DetectarCambios(IEnumerable<RegScada> data)
        => data.Zip(data.Skip(1), (a, b) => (a, b)).Where(p => p.a.Value != p.b.Value);

    private static int CantCambios(IEnumerable<RegScada> registros)
        => DetectarCambios(registros).Count();

    private static RegScada GetValue(IEnumerable<RegScada> registros)
        => DetectarCambios(registros).Select(p => p.b).First();

    private static bool UnCambio(IEnumerable<RegScada> reg)
        => CantCambios(reg) == 1;

    public static (TimeSpan horaCorte, TimeSpan freqMuestreo) ObtenerParametros(IEnumerable<RegScada> regScada)
    {
        var regOrdenados = regScada.OrderBy(m => m.FechaHora).ToList();
        var deltas = regOrdenados.Zip(regOrdenados.Skip(1), (a, b) => (b.FechaHora - a.FechaHora).TotalMinutes);
        var freqMuestreo = TimeSpan.FromMinutes(deltas.Average());

        var cambios = DetectarCambios(regScada);
        var horaCorte = cambios.Any() ? cambios.First().b.FechaHora.TimeOfDay : TimeSpan.FromHours(9);
        return (horaCorte, freqMuestreo);
    }

    public static (TimeSpan inicioVentana, TimeSpan durVentana) CalcVentana(TimeSpan horaCorte, TimeSpan freqMuestreo)
    {
        var durVentana = TimeSpan.FromHours(24) + freqMuestreo + freqMuestreo;
        var inicioVentana = horaCorte - freqMuestreo;
        return (inicioVentana, durVentana);
    }

    private static IEnumerable<(DateTime, IEnumerable<RegScada>)> AgruparEnVentanas(TimeSpan inicioVentana, TimeSpan durVentana, IEnumerable<RegScada> regScada)
    {
        var origin = new DateTime(2000, 1, 1).Add(inicioVentana);
        foreach (var g in regScada.GroupBy(m => {
                     var dt = m.FechaHora;
                     var minsDesdeOrigen = (dt - origin).TotalMinutes;
                     var minsVentana = durVentana.TotalMinutes;
                     var idx = (int)Math.Floor(minsDesdeOrigen / minsVentana);
                     return origin.AddMinutes(idx * minsVentana);
                 }).OrderBy(k => k.Key))
        {
            yield return (g.Key, g.AsEnumerable());
        }
    }

    public static IEnumerable<RegScadaDiario> GetMedicionDiaria(IEnumerable<RegScada> regScada)
    {
        var list = regScada.ToList();
        if (!list.Any()) return Enumerable.Empty<RegScadaDiario>();

        var (horaCorte, freqMuestreo) = ObtenerParametros(list);
        var (inicioVentana, durVentana) = CalcVentana(horaCorte, freqMuestreo);

        IEnumerable<RegScadaDiario> regDiarios;
        if (freqMuestreo >= TimeSpan.FromDays(0.95))
        {
            regDiarios = list.Select(r => new RegScadaDiario(r.Tag, DateOnly.FromDateTime(r.FechaHora.AddDays(-1)), r.Value));
        }
        else
        {
            regDiarios = AgruparEnVentanas(inicioVentana, durVentana, list)
                .Where(t => UnCambio(t.Item2))
                .Select(t => GetValue(t.Item2))
                .Select(reg => new RegScadaDiario(reg.Tag, DateOnly.FromDateTime(reg.FechaHora.AddDays(-1)), reg.Value));
        }

        return regDiarios
            .GroupBy(x => x.DiaGas)
            .Where(g => g.Count() == 1)
            .Select(g => g.First());
    }
}
