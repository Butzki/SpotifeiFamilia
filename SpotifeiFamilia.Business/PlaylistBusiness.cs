using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Business;

public static class PlaylistBusiness
{
    public static List<Playlist> ListarPorUsuario(int usuarioId) => PlaylistRepository.ListarPorUsuario(usuarioId);

    public static void Criar(int usuarioId, string nome)
    {
        PlaylistRepository.Criar(usuarioId, nome);
    }

    public static bool PertenceAoUsuario(int playlistId, int usuarioId) => PlaylistRepository.PertenceAoUsuario(playlistId, usuarioId);

    public static void AdicionarMusica(int playlistId, int musicaId)
    {
        PlaylistRepository.AdicionarMusica(playlistId, musicaId);
    }
}
