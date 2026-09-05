using System;
using System.Collections.Generic;
using System.Linq;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Data.Repositories;

public class MusicaRepository
{
    // Equivalente ao antigo "FiltroRestricao" em SQL: exclui músicas bloqueadas
    // (explícito geral ou artista específico) para a conta filha informada.
    // Contas pai (sem restrição cadastrada) não são afetadas.
    private static IQueryable<Musica> MusicasPermitidas(SpotifeiFamiliaContext context, int usuarioId)
    {
        return context.Musicas.Where(t =>
            !context.RestricoesConta.Any(r =>
                r.ContaFilhaId == usuarioId &&
                r.Tipo == TipoRestricao.EXPLICITO_GERAL &&
                t.Explicito) &&
            !context.RestricoesConta.Any(r =>
                r.ContaFilhaId == usuarioId &&
                r.Tipo == TipoRestricao.ARTISTA &&
                context.MusicaArtistas.Any(ta => ta.MusicaId == t.Id && ta.ArtistaId == r.ArtistaId)));
    }

    public static List<Musica> Listar(int usuarioId)
    {
        using var context = new SpotifeiFamiliaContext();
        return MusicasPermitidas(context, usuarioId).OrderBy(t => t.Titulo).ToList();
    }

    public static List<Musica> Buscar(int usuarioId, string termo)
    {
        using var context = new SpotifeiFamiliaContext();
        return MusicasPermitidas(context, usuarioId)
            .Where(t => t.Titulo.Contains(termo))
            .OrderBy(t => t.Titulo)
            .ToList();
    }

    public static Musica? BuscarPermitidaPorId(int usuarioId, int musicaId)
    {
        using var context = new SpotifeiFamiliaContext();
        return MusicasPermitidas(context, usuarioId).FirstOrDefault(t => t.Id == musicaId);
    }

    public static List<Musica> MusicasDaPlaylistPermitidas(int usuarioId, int playlistId)
    {
        using var context = new SpotifeiFamiliaContext();
        return (from pt in context.PlaylistMusicas
                where pt.PlaylistId == playlistId
                join t in MusicasPermitidas(context, usuarioId) on pt.MusicaId equals t.Id
                select t).ToList();
    }

    public static int? ObterLimiteDiario(int usuarioId)
    {
        using var context = new SpotifeiFamiliaContext();
        return context.Usuarios.Where(u => u.Id == usuarioId).Select(u => u.LimiteDiarioReproducoes).FirstOrDefault();
    }

    public static int ContarReproduzidasHoje(int usuarioId)
    {
        using var context = new SpotifeiFamiliaContext();
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        return context.ContagensDiariasReproducao
            .Where(c => c.UsuarioId == usuarioId && c.Data == hoje)
            .Select(c => c.Quantidade)
            .FirstOrDefault();
    }

    // Registra a reprodução no histórico e soma na contagem diária do usuário
    // (cria a linha do dia se ainda não existir) — equivalente ao antigo
    // "INSERT ... ON DUPLICATE KEY UPDATE quantidade = quantidade + 1".
    public static void RegistrarReproducao(int usuarioId, int musicaId)
    {
        using var context = new SpotifeiFamiliaContext();

        context.HistoricosReproducao.Add(new HistoricoReproducao
        {
            UsuarioId = usuarioId,
            MusicaId = musicaId,
            DataHora = DateTime.Now
        });

        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var contagem = context.ContagensDiariasReproducao
            .FirstOrDefault(c => c.UsuarioId == usuarioId && c.Data == hoje);

        if (contagem == null)
        {
            context.ContagensDiariasReproducao.Add(new ContagemDiariaReproducao
            {
                UsuarioId = usuarioId,
                Data = hoje,
                Quantidade = 1
            });
        }
        else
        {
            contagem.Quantidade++;
        }

        context.SaveChanges();
    }
}
