using System;
using Microsoft.EntityFrameworkCore;
using ScadaPI.CSharp.Data;
using static ScadaPI.CSharp.InsertConsumoHorario;
using static ScadaPI.CSharp.InsertPoderCalorificoDiario;
using static ScadaPI.CSharp.InsertPoderCalorificoHorario;

namespace ScadaPI.CSharp;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello from C#");

        var options = new DbContextOptionsBuilder<ScadaDbContext>()
            .UseSqlServer(ScadaDbContext.DefaultConnection)
            .Options;

        using var ctx = new ScadaDbContext(options);
        foreach (var key in ctx.TagScadas.Select(t => t.Tag))
            Console.WriteLine(key);

      InsertConsumoDiario.ImportarConsumoDiarioPorTag(PiClient.ConnectionString);


      ImportarPoderCalorificoDiarioPorTag(PiClient.ConnectionString);

      ImportarConsumoHorarioPorTag(PiClient.ConnectionString);
      ImportarPoderCalorificoHorarioPorTag(PiClient.ConnectionString);
    }
}
