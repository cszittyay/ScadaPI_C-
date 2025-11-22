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

       
      //InsertConsumoDiario.ImportarConsumoDiarioPorTag(PiClient.ConnectionString);


      //ImportarPoderCalorificoDiarioPorTag(PiClient.ConnectionString);

      ImportarConsumoHorarioPorTag(PiClient.ConnectionString);
      ImportarPoderCalorificoHorarioPorTag(PiClient.ConnectionString);
    }
}
