using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Model;
using SpotifeiFamilia.Views;

namespace SpotifeiFamilia.Controllers;

public class FamiliaController(int usuarioId)
{
    private readonly FamiliaView view = new();

    // Ponto de entrada: reautentica e checa se o usuário pode gerenciar família
    public void Executar()
    {
        if (!Reautenticar())
        {
            view.SenhaIncorretaVoltando();
            return;
        }

        if (!PodeGerenciarFamilia(out string motivo))
        {
            view.Motivo(motivo);
            return;
        }

        bool ativo = true;
        while (ativo)
        {
            string opcao = view.ExibirMenuFamilia();

            switch (opcao)
            {
                case "1": ListarDependentes(); break;
                case "2": AdicionarDependente(); break;
                case "3": RemoverDependente(); break;
                case "4": new RestricaoController(usuarioId).Executar(); break;
                case "5": VerHistoricoDependente(); break;
                case "6": ativo = false; break;
                default: view.OpcaoInvalida(); break;
            }
        }
    }

    // Pede a senha novamente antes de liberar acesso ao painel de família
    private bool Reautenticar()
    {
        string senha = view.LerSenhaConfirmacao();

        try
        {
            return UsuarioRepository.VerificarSenha(usuarioId, senha);
        }
        catch (Exception ex)
        {
            view.Erro("verificar senha", ex.Message);
            return false;
        }
    }

    // Regra de negócio: só titular (sem responsavel_id) com plano PREMIUM pode gerenciar família
    private bool PodeGerenciarFamilia(out string motivo)
    {
        motivo = "";

        try
        {
            var (ehDependente, nomePlano) = FamiliaRepository.ObterInfoPermissao(usuarioId);

            if (ehDependente)
            {
                motivo = "Contas dependentes não podem gerenciar o Spotifei Família.";
                return false;
            }

            if (nomePlano != nameof(NomePlano.PREMIUM))
            {
                motivo = "O Spotifei Família está disponível apenas para o plano PREMIUM.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            motivo = "Erro ao verificar permissões: " + ex.Message;
            return false;
        }
    }

    private void ListarDependentes()
    {
        try
        {
            view.ExibirDependentes(FamiliaRepository.ListarDependentes(usuarioId));
        }
        catch (Exception ex)
        {
            view.Erro("listar membros da família", ex.Message);
        }
    }

    private void AdicionarDependente()
    {
        string? nome = view.LerNomeDependente();
        if (nome == null) { view.CadastroDependenteCancelado(); return; }

        // E-mail é exigido porque o login autentica por e-mail + senha, igual para contas pai e filho.
        string? email = view.LerEmailDependente();
        if (email == null) { view.CadastroDependenteCancelado(); return; }

        string? senha = view.LerSenhaDependente();
        if (senha == null) { view.CadastroDependenteCancelado(); return; }

        try
        {
            FamiliaRepository.AdicionarDependente(usuarioId, nome, email, senha);
            view.DependenteAdicionado();
        }
        catch (Exception ex)
        {
            view.Erro("adicionar membro", ex.Message);
        }
    }

    private void RemoverDependente()
    {
        int? id = view.LerIdRemover();
        if (id == null) return;

        try
        {
            view.DependenteRemovido(FamiliaRepository.RemoverDependente(id.Value, usuarioId));
        }
        catch (Exception ex)
        {
            view.Erro("remover membro", ex.Message);
        }
    }

    // Lista os filhos do responsável logado e pede pra escolher um, confirmando
    // que o ID pertence a essa família antes de liberar.
    internal int? SelecionarDependente()
    {
        try
        {
            var dependentes = FamiliaRepository.ListarDependentes(usuarioId);
            view.ExibirDependentesParaSelecao(dependentes);
            if (dependentes.Count == 0) return null;

            int? contaFilhaId = view.LerIdMembro();
            if (contaFilhaId == null) return null;

            if (!FamiliaRepository.EhDependenteDe(contaFilhaId.Value, usuarioId))
            {
                view.NaoEhMembroDaFamilia();
                return null;
            }

            return contaFilhaId;
        }
        catch (Exception ex)
        {
            view.Erro("validar membro", ex.Message);
            return null;
        }
    }

    private void VerHistoricoDependente()
    {
        int? contaFilhaId = SelecionarDependente();
        if (contaFilhaId == null) return;

        try
        {
            view.ExibirHistorico(HistoricoRepository.ListarPorUsuario(contaFilhaId.Value));
        }
        catch (Exception ex)
        {
            view.Erro("consultar histórico", ex.Message);
        }
    }
}
