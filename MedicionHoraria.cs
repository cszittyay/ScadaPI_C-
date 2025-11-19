using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using static ScadaPI.CSharp.DbContextPI;
using static ScadaPI.CSharp.PiClient;

namespace ScadaPI.CSharp;

public static class MedicionHoraria

{
    private static IEnumerable<(RegScada a, RegScada b)> DetectarCambios(IEnumerable<RegScada> data)
        => data.Zip(data.Skip(1), (a, b) => (a, b)).Where(p => p.a.Value != p.b.Value);

    private static int CantCambios(IEnumerable<RegScada> registros)
        => DetectarCambios(registros).Count();

   

    public static IEnumerable<RegScada> GetMedicionHoraria(IEnumerable<RegScada> regScada)
    {
        var list = regScada.ToList();
        if (!list.Any()) return Enumerable.Empty<RegScada>();
        return DetectarCambios(regScada).Select(tuple => tuple.b);
    }  
      
}
