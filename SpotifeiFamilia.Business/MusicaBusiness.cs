using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Business;

public static class MusicaBusiness
{
    public static List<Musica> Listar(int usuarioId)
    {
        Validador.ValidarId(usuarioId, "Usuário");
        return MusicaRepository.Listar(usuarioId);
    }

    public static List<Musica> Buscar(int usuarioId, string termo)
    {
        Validador.ValidarId(usuarioId, "Usuário");
        return MusicaRepository.Buscar(usuarioId, Validador.NormalizarTermoBusca(termo));
    }

    /// <summary>Devolve a música só se ela existir E estiver liberada para este usuário (restrições da família aplicadas).</summary>
    public static Musica? BuscarPermitidaPorId(int usuarioId, int musicaId)
    {
        if (usuarioId <= 0 || musicaId <= 0)
            return null;

        return MusicaRepository.BuscarPermitidaPorId(usuarioId, musicaId);
    }

    public static int? ObterLimiteDiario(int usuarioId) => MusicaRepository.ObterLimiteDiario(usuarioId);

    public static int ContarReproduzidasHoje(int usuarioId) => MusicaRepository.ContarReproduzidasHoje(usuarioId);

    /// <summary>
    /// Registra a reprodução aplicando TODAS as regras aqui dentro (conta ativa, música liberada,
    /// limite diário), e não só na tela: a WebApi chama este método direto e, antes, escapava do
    /// limite do controle parental. A checagem do limite e o registro são passos separados no
    /// banco; duas reproduções simultâneas da mesma conta podem, no pior caso, passar 1 acima do limite.
    /// </summary>
    public static void RegistrarReproducao(int usuarioId, int musicaId)
    {
        Validador.ValidarId(usuarioId, "Usuário");
        Validador.ValidarId(musicaId, "Música");

        var usuario = UsuarioRepository.BuscarPorId(usuarioId)
            ?? throw new RegraNegocioException("Usuário não encontrado.");

        if (usuario.Bloqueado || (usuario.BloqueadoAte.HasValue && usuario.BloqueadoAte.Value > DateTime.Now))
            throw new RegraNegocioException("Esta conta está bloqueada.");

        if (MusicaRepository.BuscarPermitidaPorId(usuarioId, musicaId) == null)
            throw new RegraNegocioException("Música não encontrada ou indisponível para esta conta.");

        if (usuario.LimiteDiarioReproducoes.HasValue &&
            MusicaRepository.ContarReproduzidasHoje(usuarioId) >= usuario.LimiteDiarioReproducoes.Value)
        {
            throw new RegraNegocioException($"Limite diário de {usuario.LimiteDiarioReproducoes.Value} música(s) atingido.");
        }

        MusicaRepository.RegistrarReproducao(usuarioId, musicaId);
    }

    public static List<Musica> MusicasDaPlaylistPermitidas(int usuarioId, int playlistId)
    {
        Validador.ValidarId(usuarioId, "Usuário");
        Validador.ValidarId(playlistId, "Playlist");

        if (!PlaylistRepository.PertenceAoUsuario(playlistId, usuarioId))
            throw new RegraNegocioException("Playlist não encontrada.");

        return MusicaRepository.MusicasDaPlaylistPermitidas(usuarioId, playlistId);
    }
}
