using SpotifeiFamilia.Business;
using SpotifeiFamilia.Views;

namespace SpotifeiFamilia.Controllers;

public class RestricaoController(int usuarioId)
{
    private readonly RestricaoView view = new();

    public void Executar()
    {
        int? contaFilhaId = new FamiliaController(usuarioId).SelecionarDependente();
        if (contaFilhaId == null) return;

        bool ativo = true;
        while (ativo)
        {
            string opcao = view.ExibirMenuRestricoes();

            switch (opcao)
            {
                case "1": ListarRestricoes(contaFilhaId.Value); break;
                case "2": BloquearArtista(contaFilhaId.Value); break;
                case "3": DesbloquearArtista(contaFilhaId.Value); break;
                case "4": BloquearExplicito(contaFilhaId.Value); break;
                case "5": DesbloquearExplicito(contaFilhaId.Value); break;
                case "6": DefinirLimiteDiario(contaFilhaId.Value); break;
                case "7": ativo = false; break;
                default: view.OpcaoInvalida(); break;
            }
        }
    }

    private void ListarRestricoes(int contaFilhaId)
    {
        try
        {
            view.ExibirRestricoes(RestricaoBusiness.ListarRestricoes(usuarioId, contaFilhaId));

            int? limiteDiario = RestricaoBusiness.ObterLimiteDiario(usuarioId, contaFilhaId);
            if (limiteDiario == null)
            {
                view.SemLimiteDiario();
            }
            else
            {
                int reproduzidasHoje = RestricaoBusiness.ContarReproduzidasHoje(usuarioId, contaFilhaId);
                view.ExibirLimiteDiario(reproduzidasHoje, limiteDiario.Value);
            }
        }
        catch (Exception ex)
        {
            view.Erro("listar restrições", ex.Message);
        }
    }

    private void BloquearArtista(int contaFilhaId)
    {
        string nome = view.LerNomeArtista();

        try
        {
            var encontrados = FamiliaBusiness.BuscarArtistasPorNome(nome);
            if (encontrados.Count == 0)
            {
                view.ArtistaNaoEncontrado();
                return;
            }

            int artistaId;
            if (encontrados.Count > 1)
            {
                view.ExibirArtistasEncontrados(encontrados);
                int? idEscolhido = view.LerIdArtistaEscolhido();
                if (idEscolhido == null || !encontrados.Exists(a => a.Id == idEscolhido))
                {
                    view.IdInvalido();
                    return;
                }
                artistaId = idEscolhido.Value;
            }
            else
            {
                artistaId = encontrados[0].Id;
            }

            if (RestricaoBusiness.ArtistaJaBloqueado(usuarioId, contaFilhaId, artistaId))
            {
                view.ArtistaJaBloqueado();
                return;
            }

            RestricaoBusiness.BloquearArtista(usuarioId, contaFilhaId, artistaId);
            view.ArtistaBloqueado();
        }
        catch (Exception ex)
        {
            view.Erro("bloquear artista", ex.Message);
        }
    }

    private void DesbloquearArtista(int contaFilhaId)
    {
        int? artistaId = view.LerIdArtistaDesbloquear();
        if (artistaId == null) return;

        try
        {
            int linhas = RestricaoBusiness.DesbloquearArtista(usuarioId, contaFilhaId, artistaId.Value);
            view.ArtistaDesbloqueado(linhas > 0);
        }
        catch (Exception ex)
        {
            view.Erro("desbloquear artista", ex.Message);
        }
    }

    private void BloquearExplicito(int contaFilhaId)
    {
        try
        {
            if (RestricaoBusiness.ExplicitoJaBloqueado(usuarioId, contaFilhaId))
            {
                view.ExplicitoJaBloqueado();
                return;
            }

            RestricaoBusiness.BloquearExplicito(usuarioId, contaFilhaId);
            view.ExplicitoBloqueado();
        }
        catch (Exception ex)
        {
            view.Erro("bloquear conteúdo explícito", ex.Message);
        }
    }

    private void DesbloquearExplicito(int contaFilhaId)
    {
        try
        {
            int linhas = RestricaoBusiness.DesbloquearExplicito(usuarioId, contaFilhaId);
            view.ExplicitoDesbloqueado(linhas > 0);
        }
        catch (Exception ex)
        {
            view.Erro("desbloquear conteúdo explícito", ex.Message);
        }
    }

    private void DefinirLimiteDiario(int contaFilhaId)
    {
        try
        {
            view.ExibirLimiteAtual(RestricaoBusiness.ObterLimiteDiario(usuarioId, contaFilhaId));

            int? novoLimite = view.LerNovoLimite();
            if (novoLimite == null) return;

            RestricaoBusiness.DefinirLimiteDiario(usuarioId, contaFilhaId, novoLimite == 0 ? null : novoLimite);
            view.LimiteAtualizado();
        }
        catch (Exception ex)
        {
            view.Erro("definir limite diário", ex.Message);
        }
    }
}
