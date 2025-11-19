using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ScadaPI.CSharp;

public static class PiClient
{
    public const string ConnectionString = "Server=192.168.100.115;Database=master;User ID=usrGGN;Password=25GNN25LqZSK;TrustServerCertificate=True;";

    public record RegScada(string Tag, DateTime FechaHora, double Value);
    public record RegScadaDiario(string Tag, DateOnly DiaGas, double Value);

    public static IEnumerable<RegScada> GetDataSeq(string connectionString, string sql)
    {
        using var conn = new SqlConnection(connectionString);
        using var cmd = new SqlCommand(sql, conn);
        conn.Open();
        using var rd = cmd.ExecuteReader(CommandBehavior.CloseConnection);
        while (rd.Read())
        {
            var tag = rd.GetString(0);
            var time = rd.GetDateTime(1);
            var value = rd.IsDBNull(2) ? -1.0 : rd.GetDouble(2);
            if (Math.Abs(value - (-1.0)) > double.Epsilon)
                yield return new RegScada(tag, time, value);
        }
    }

    public static IEnumerable<RegScada> GetPi(string connString, string tag, int daysBack)
    {
        var startIso = DateTime.UtcNow.AddDays(-(daysBack + 1)).ToString("s");
        var sql = $"""
                SELECT tag,[time],value
                FROM OPENQUERY(PI_CONSOLIDADOR_GGN, '
                    SELECT tag, time, value
                    FROM piarchive..picomp
                    WHERE tag = ''{tag}''
                    AND time > ''{startIso}''
                    order by time asc
                ')
                """;
        return GetDataSeq(connString, sql);
    }
}
