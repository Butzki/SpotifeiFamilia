using System;
using System.Collections.Generic;
using System.Linq;

namespace SpotifeiFamilia.Data.Repositories;

public class HistoricoRepository
{
    public static List<(DateTime DataHora, string Titulo)> ListarPorUsuario(int usuarioId)
    {
        using var context = new SpotifeiFamiliaContext();
        return (from h in context.HistoricosReproducao
                join t in context.Musicas on h.MusicaId equals t.Id
                where h.UsuarioId == usuarioId
                orderby h.DataHora descending
                select new { h.DataHora, t.Titulo })
               .AsEnumerable()
               .Select(x => (x.DataHora, x.Titulo))
               .ToList();
    }
}
