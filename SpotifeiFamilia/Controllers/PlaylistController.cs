using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Views;

namespace SpotifeiFamilia.Controllers;

public class PlaylistController(int usuarioId)
{
    private readonly PlaylistView view = new();

    public void MenuPlaylists()
    {
        bool ativo = true;

        while (ativo)
        {
            string opcao = view.ExibirMenuPlaylists();

            switch (opcao)
            {
                case "1": ListarPlaylists(); break;
                case "2": CriarPlaylist(); break;
                case "3": VerMusicasPlaylist(); break;
                case "4": AdicionarMusicaPlaylist(); break;
                case "5": ativo = false; break;
                default: view.OpcaoInvalida(); break;
            }
        }
    }

    private void ListarPlaylists()
    {
        try
        {
            view.ExibirPlaylists(PlaylistRepository.ListarPorUsuario(usuarioId));
        }
        catch (Exception ex)
        {
            view.Erro("listar playlists", ex.Message);
        }
    }

    private void CriarPlaylist()
    {
        string nome = view.LerNomePlaylist();

        try
        {
            PlaylistRepository.Criar(usuarioId, nome);
            view.PlaylistCriada();
        }
        catch (Exception ex)
        {
            view.Erro("criar playlist", ex.Message);
        }
    }

    private void VerMusicasPlaylist()
    {
        int? playlistId = view.LerIdPlaylist();
        if (playlistId == null) return;

        try
        {
            if (!PlaylistRepository.PertenceAoUsuario(playlistId.Value, usuarioId))
            {
                view.PlaylistNaoEncontrada();
                return;
            }

            view.ExibirMusicasPlaylist(MusicaRepository.MusicasDaPlaylistPermitidas(usuarioId, playlistId.Value));
        }
        catch (Exception ex)
        {
            view.Erro("visualizar músicas da playlist", ex.Message);
        }
    }

    private void AdicionarMusicaPlaylist()
    {
        int? playlistId = view.LerIdPlaylist();
        if (playlistId == null) return;

        int? trackId = view.LerIdMusica();
        if (trackId == null) return;

        try
        {
            if (!PlaylistRepository.PertenceAoUsuario(playlistId.Value, usuarioId))
            {
                view.PlaylistNaoEncontrada();
                return;
            }

            PlaylistRepository.AdicionarMusica(playlistId.Value, trackId.Value);
            view.MusicaAdicionadaPlaylist();
        }
        catch (Exception ex)
        {
            view.Erro("adicionar música à playlist", ex.Message);
        }
    }
}
