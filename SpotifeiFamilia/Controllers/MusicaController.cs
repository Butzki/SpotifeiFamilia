using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Views;

namespace SpotifeiFamilia.Controllers;

public class MusicaController(int usuarioId)
{
    private readonly MusicaView view = new();

    public void VerMusicas()
    {
        try
        {
            view.ExibirMusicas(MusicaRepository.Listar(usuarioId));
        }
        catch (Exception ex)
        {
            view.Erro("listar músicas", ex.Message);
        }
    }

    public void BuscarMusica()
    {
        string busca = view.LerTermoBusca();

        try
        {
            view.ExibirResultadosBusca(MusicaRepository.Buscar(usuarioId, busca));
        }
        catch (Exception ex)
        {
            view.Erro("buscar música", ex.Message);
        }
    }

    // Registra a reprodução no histórico, respeitando as restrições de conteúdo
    // e o limite diário configurados para essa conta.
    public void ReproduzirMusica()
    {
        int? trackId = view.LerIdMusica();
        if (trackId == null) return;

        try
        {
            int? limiteDiario = MusicaRepository.ObterLimiteDiario(usuarioId);
            if (limiteDiario != null)
            {
                int reproduzidasHoje = MusicaRepository.ContarReproduzidasHoje(usuarioId);
                if (reproduzidasHoje >= limiteDiario)
                {
                    view.LimiteDiarioAtingido(limiteDiario.Value);
                    return;
                }
            }

            var musica = MusicaRepository.BuscarPermitidaPorId(usuarioId, trackId.Value);
            if (musica == null)
            {
                view.MusicaNaoEncontrada();
                return;
            }

            MusicaRepository.RegistrarReproducao(usuarioId, trackId.Value);
            view.Reproduzindo(musica.Titulo);
        }
        catch (Exception ex)
        {
            view.Erro("reproduzir música", ex.Message);
        }
    }
}
