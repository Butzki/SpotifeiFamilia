using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Business;

public static class MusicaBusiness
{
    public static List<Musica> Listar(int usuarioId) => MusicaRepository.Listar(usuarioId);

    public static List<Musica> Buscar(int usuarioId, string termo) => MusicaRepository.Buscar(usuarioId, termo);

    public static Musica? BuscarPermitidaPorId(int usuarioId, int musicaId) => MusicaRepository.BuscarPermitidaPorId(usuarioId, musicaId);

    public static int? ObterLimiteDiario(int usuarioId) => MusicaRepository.ObterLimiteDiario(usuarioId);

    public static int ContarReproduzidasHoje(int usuarioId) => MusicaRepository.ContarReproduzidasHoje(usuarioId);

    public static void RegistrarReproducao(int usuarioId, int musicaId)
    {
        MusicaRepository.RegistrarReproducao(usuarioId, musicaId);
    }

    public static List<Musica> MusicasDaPlaylistPermitidas(int usuarioId, int playlistId)
    {
        return MusicaRepository.MusicasDaPlaylistPermitidas(usuarioId, playlistId);
    }
}
