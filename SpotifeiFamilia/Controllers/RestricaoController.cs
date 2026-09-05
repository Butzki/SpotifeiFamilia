using SpotifeiFamilia.Data.Repositories;
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
            view.ExibirRestricoes(FamiliaRepository.ListarRestricoes(contaFilhaId));

            int? limiteDiario = MusicaRepository.ObterLimiteDiario(contaFilhaId);
            if (limiteDiario == null)
            {
                view.SemLimiteDiario();
            }
            else
            {
                int reproduzidasHoje = MusicaRepository.ContarReproduzidasHoje(contaFilhaId);
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
            var encontrados = FamiliaRepository.BuscarArtistasPorNome(nome);
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

            if (FamiliaRepository.ArtistaJaBloqueado(contaFilhaId, artistaId))
            {
                view.ArtistaJaBloqueado();
                return;
            }

            FamiliaRepository.BloquearArtista(contaFilhaId, artistaId);
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
            int linhas = FamiliaRepository.DesbloquearArtista(contaFilhaId, artistaId.Value);
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
            if (FamiliaRepository.ExplicitoJaBloqueado(contaFilhaId))
            {
                view.ExplicitoJaBloqueado();
                return;
            }

            FamiliaRepository.BloquearExplicito(contaFilhaId);
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
            int linhas = FamiliaRepository.DesbloquearExplicito(contaFilhaId);
            view.ExplicitoDesbloqueado(linhas > 0);
        }
        catch (Exception ex)
        {
            view.Erro("desbloquear conteúdo explícito", ex.Message);
        }
    }

    // Define (ou remove) o limite diário de reprodução de músicas para a conta filha.
    private void DefinirLimiteDiario(int contaFilhaId)
    {
        try
        {
            view.ExibirLimiteAtual(MusicaRepository.ObterLimiteDiario(contaFilhaId));

            int? novoLimite = view.LerNovoLimite();
            if (novoLimite == null) return;

            // 0 significa "sem limite" (grava NULL); qualquer valor > 0 é o novo limite
            FamiliaRepository.DefinirLimiteDiario(contaFilhaId, novoLimite == 0 ? null : novoLimite);
            view.LimiteAtualizado();
        }
        catch (Exception ex)
        {
            view.Erro("definir limite diário", ex.Message);
        }
    }
}
