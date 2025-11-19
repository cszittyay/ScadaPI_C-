using System;

namespace ScadaPI.CSharp.Data;

public class TagScada
{
    public int Id_Tag { get; set; }
    public string Tag { get; set; } = string.Empty;
    public bool Importar { get; set; }
    public string Frecuencia { get; set; }
    public string  Unidad { get; set; }
    public decimal FactorAjuste { get; set; }
    public DateTime UltimaFechaHora { get; set; }
    public DateTime FechaHoraError { get; set; }
}

public class ScadaConsumoDiario
{
    public int Id_TagScada { get; set; }
    public DateTime DiaGas { get; set; }
    public decimal Consumo { get; set; }

    public TagScada? Tag { get; set; }
}

public class ScadaConsumoDiarioError
{
    public int Id_TagScada { get; set; }
    public DateTime FechaHora { get; set; }
    public decimal Consumo { get; set; }

    public TagScada? Tag { get; set; }
}


// Poder calorifico diario entities

public class ScadaPoderCalorificoDiario
{
    public int Id_TagScada { get; set; }
    public DateTime DiaGas { get; set; }
    public decimal PoderCalorifico { get; set; }

    public TagScada? Tag { get; set; }
}

public class ScadaPoderCalorificoDiarioError
{
    public int Id_TagScada { get; set; }
    public DateTime FechaHora { get; set; }
    public decimal PoderCalorifico { get; set; }

    public TagScada? Tag { get; set; }
}
