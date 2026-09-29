using SpotifeiFamilia.Business;
using SpotifeiFamilia.Views;

namespace SpotifeiFamilia.Controllers;

public class FamiliaController(int usuarioId)
{
    private readonly FamiliaView view = new();

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

    private bool Reautenticar()
    {
        string senha = view.LerSenhaConfirmacao();

        try
        {
            return FamiliaBusiness.Reautenticar(usuarioId, senha);
        }
        catch (Exception ex)
        {
            view.Erro("verificar senha", ex.Message);
            return false;
        }
    }

    private bool PodeGerenciarFamilia(out string motivo)
    {
        try
        {
            return FamiliaBusiness.PodeGerenciarFamilia(usuarioId, out motivo);
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
            view.ExibirDependentes(FamiliaBusiness.ListarDependentes(usuarioId));
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

        string? email = view.LerEmailDependente();
        if (email == null) { view.CadastroDependenteCancelado(); return; }

        string? senha = view.LerSenhaDependente();
        if (senha == null) { view.CadastroDependenteCancelado(); return; }

        try
        {
            FamiliaBusiness.AdicionarDependente(usuarioId, nome, email, senha);
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
            view.DependenteRemovido(FamiliaBusiness.RemoverDependente(id.Value, usuarioId));
        }
        catch (Exception ex)
        {
            view.Erro("remover membro", ex.Message);
        }
    }

    internal int? SelecionarDependente()
    {
        try
        {
            var dependentes = FamiliaBusiness.ListarDependentes(usuarioId);
            view.ExibirDependentesParaSelecao(dependentes);
            if (dependentes.Count == 0) return null;

            int? contaFilhaId = view.LerIdMembro();
            if (contaFilhaId == null) return null;

            if (!FamiliaBusiness.EhDependenteDe(contaFilhaId.Value, usuarioId))
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
            view.ExibirHistorico(FamiliaBusiness.ListarHistoricoPorUsuario(contaFilhaId.Value));
        }
        catch (Exception ex)
        {
            view.Erro("consultar histórico", ex.Message);
        }
    }
}
