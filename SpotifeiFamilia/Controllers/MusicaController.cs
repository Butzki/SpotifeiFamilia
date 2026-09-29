using SpotifeiFamilia.Business;
using SpotifeiFamilia.Views;

namespace SpotifeiFamilia.Controllers;

public class MusicaController(int usuarioId)
{
    private readonly MusicaView view = new();

    public void VerMusicas()
    {
        try
        {
            view.ExibirMusicas(MusicaBusiness.Listar(usuarioId));
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
            view.ExibirResultadosBusca(MusicaBusiness.Buscar(usuarioId, busca));
        }
        catch (Exception ex)
        {
            view.Erro("buscar música", ex.Message);
        }
    }

    public void ReproduzirMusica()
    {
        int? trackId = view.LerIdMusica();
        if (trackId == null) return;

        try
        {
            int? limiteDiario = MusicaBusiness.ObterLimiteDiario(usuarioId);
            if (limiteDiario != null)
            {
                int reproduzidasHoje = MusicaBusiness.ContarReproduzidasHoje(usuarioId);
                if (reproduzidasHoje >= limiteDiario)
                {
                    view.LimiteDiarioAtingido(limiteDiario.Value);
                    return;
                }
            }

            var musica = MusicaBusiness.BuscarPermitidaPorId(usuarioId, trackId.Value);
            if (musica == null)
            {
                view.MusicaNaoEncontrada();
                return;
            }

            MusicaBusiness.RegistrarReproducao(usuarioId, trackId.Value);
            view.Reproduzindo(musica.Titulo);
        }
        catch (Exception ex)
        {
            view.Erro("reproduzir música", ex.Message);
        }
    }
}
