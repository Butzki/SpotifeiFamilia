using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Views;

public class FamiliaView
{
    public string LerSenhaConfirmacao()
    {
        Console.Write("Para continuar, confirme sua senha: ");
        return Console.ReadLine() ?? "";
    }

    public void SenhaIncorretaVoltando() => Console.WriteLine("Senha incorreta. Voltando ao menu principal.");

    public void Motivo(string motivo) => Console.WriteLine(motivo);

    public string ExibirMenuFamilia()
    {
        Console.WriteLine("\n--- SPOTIFEI FAMÍLIA ---");
        Console.WriteLine("1. Ver membros da família");
        Console.WriteLine("2. Adicionar membro");
        Console.WriteLine("3. Remover membro");
        Console.WriteLine("4. Gerenciar restrições (artistas/explícito)");
        Console.WriteLine("5. Ver histórico de reprodução de um membro");
        Console.WriteLine("6. Voltar");
        Console.Write("Escolha: ");
        return Console.ReadLine() ?? "";
    }

    public void OpcaoInvalida() => Console.WriteLine("Opção inválida.");

    public void ExibirDependentes(List<Usuario> dependentes)
    {
        Console.WriteLine("\n--- MEMBROS DA FAMÍLIA ---");
        if (dependentes.Count == 0)
        {
            Console.WriteLine("Nenhum membro cadastrado ainda.");
            return;
        }
        foreach (var d in dependentes)
            Console.WriteLine($"[{d.Id}] {d.NomeUsuario}");
    }

    public string? LerNomeDependente() => EntradaConsole.LerOuCancelar("Nome do novo membro", s => !string.IsNullOrWhiteSpace(s), "Nome inválido.");

    public string? LerEmailDependente() => EntradaConsole.LerOuCancelar("E-mail do novo membro", s => System.Text.RegularExpressions.Regex.IsMatch(s, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"), "E-mail inválido.");

    public string? LerSenhaDependente() => EntradaConsole.LerOuCancelar("Senha para o novo membro", s => s.Length >= 6, "A senha deve ter pelo menos 6 caracteres.");

    public void CadastroDependenteCancelado() => Console.WriteLine("Cadastro de membro cancelado.");

    public void DependenteAdicionado() => Console.WriteLine("Membro adicionado com sucesso!");

    public void Erro(string acao, string mensagem) => Console.WriteLine($"Erro ao {acao}: {mensagem}");

    public int? LerIdRemover()
    {
        Console.Write("ID do membro a remover: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("ID inválido.");
            return null;
        }
        return id;
    }

    public void DependenteRemovido(bool removido) => Console.WriteLine(removido
        ? "Membro removido com sucesso."
        : "Nenhum membro encontrado com esse ID na sua família.");

    public void ExibirDependentesParaSelecao(List<Usuario> dependentes)
    {
        Console.WriteLine("\n--- SELECIONE UM MEMBRO DA FAMÍLIA ---");
        if (dependentes.Count == 0)
        {
            Console.WriteLine("Nenhum membro cadastrado ainda.");
            return;
        }
        foreach (var d in dependentes)
            Console.WriteLine($"[{d.Id}] {d.NomeUsuario}");
    }

    public int? LerIdMembro()
    {
        Console.Write("ID do membro: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("ID inválido.");
            return null;
        }
        return id;
    }

    public void NaoEhMembroDaFamilia() => Console.WriteLine("Esse ID não é um membro da sua família.");

    public void ExibirHistorico(List<(DateTime DataHora, string Titulo)> historico)
    {
        Console.WriteLine("\n--- HISTÓRICO DE REPRODUÇÃO ---");
        if (historico.Count == 0)
        {
            Console.WriteLine("Nenhuma reprodução registrada ainda.");
            return;
        }
        foreach (var (dataHora, titulo) in historico)
            Console.WriteLine($"[{dataHora:dd/MM/yyyy HH:mm}] {titulo}");
    }
}
