using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Business;

public static class PlaylistBusiness
{
    public static List<Playlist> ListarPorUsuario(int usuarioId)
    {
        Validador.ValidarId(usuarioId, "Usuário");
        return PlaylistRepository.ListarPorUsuario(usuarioId);
    }

    public static void Criar(int usuarioId, string nome)
    {
        Validador.ValidarId(usuarioId, "Usuário");
        nome = Validador.NormalizarNomePlaylist(nome);

        if (UsuarioRepository.BuscarPorId(usuarioId) == null)
            throw new RegraNegocioException("Usuário não encontrado.");

        // Nome único por usuário (sem diferenciar maiúsculas): evita playlists indistinguíveis na listagem.
        bool jaExiste = PlaylistRepository.ListarPorUsuario(usuarioId)
            .Any(p => string.Equals(p.NomePlaylist, nome, StringComparison.OrdinalIgnoreCase));

        if (jaExiste)
            throw new RegraNegocioException("Você já tem uma playlist com esse nome.");

        PlaylistRepository.Criar(usuarioId, nome);
    }

    public static bool PertenceAoUsuario(int playlistId, int usuarioId)
    {
        if (playlistId <= 0 || usuarioId <= 0)
            return false;

        return PlaylistRepository.PertenceAoUsuario(playlistId, usuarioId);
    }

    /// <summary>
    /// Só o dono mexe na playlist, só entra música que exista E esteja liberada para o dono (uma
    /// conta filha não coloca em playlist o que o responsável bloqueou) e a mesma música não entra duas vezes.
    /// </summary>
    public static void AdicionarMusica(int usuarioId, int playlistId, int musicaId)
    {
        Validador.ValidarId(usuarioId, "Usuário");
        Validador.ValidarId(playlistId, "Playlist");
        Validador.ValidarId(musicaId, "Música");

        if (!PlaylistRepository.PertenceAoUsuario(playlistId, usuarioId))
            throw new RegraNegocioException("Playlist não encontrada.");

        if (MusicaRepository.BuscarPermitidaPorId(usuarioId, musicaId) == null)
            throw new RegraNegocioException("Música não encontrada ou indisponível para esta conta.");

        // A mesma restrição vale na listagem, então uma música permitida que já está na playlist aparece aqui.
        if (MusicaRepository.MusicasDaPlaylistPermitidas(usuarioId, playlistId).Any(m => m.Id == musicaId))
            throw new RegraNegocioException("Esta música já está na playlist.");

        PlaylistRepository.AdicionarMusica(playlistId, musicaId);
    }
}
