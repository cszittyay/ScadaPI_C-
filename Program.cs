using System;
using Microsoft.EntityFrameworkCore;
using ScadaPI.CSharp.Data;
using static ScadaPI.CSharp.InsertConsumoDiario;
using static ScadaPI.CSharp.InsertPoderCalorificoDiario;

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

      //  ImportarConsumoDiarioPorTag(PiClient.ConnectionString);


        ImportarPoderCalorificoDiarioPorTag(PiClient.ConnectionString);
    }
}
