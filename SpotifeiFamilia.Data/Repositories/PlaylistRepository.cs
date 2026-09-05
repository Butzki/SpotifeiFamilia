using System.Collections.Generic;
using System.Linq;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Data.Repositories;

public class PlaylistRepository
{
    public static List<Playlist> ListarPorUsuario(int usuarioId)
    {
        using var context = new SpotifeiFamiliaContext();
        return context.Playlists.Where(p => p.UsuarioId == usuarioId).ToList();
    }

    public static void Criar(int usuarioId, string nome)
    {
        using var context = new SpotifeiFamiliaContext();
        context.Playlists.Add(new Playlist { UsuarioId = usuarioId, NomePlaylist = nome });
        context.SaveChanges();
    }

    public static bool PertenceAoUsuario(int playlistId, int usuarioId)
    {
        using var context = new SpotifeiFamiliaContext();
        return context.Playlists.Any(p => p.Id == playlistId && p.UsuarioId == usuarioId);
    }

    public static void AdicionarMusica(int playlistId, int musicaId)
    {
        using var context = new SpotifeiFamiliaContext();
        context.PlaylistMusicas.Add(new PlaylistMusica { PlaylistId = playlistId, MusicaId = musicaId });
        context.SaveChanges();
    }
}
