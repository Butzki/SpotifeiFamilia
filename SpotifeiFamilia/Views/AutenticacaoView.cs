using System.Text.RegularExpressions;

namespace SpotifeiFamilia.Views;

public class AutenticacaoView
{
    public void CabecalhoCadastro()
    {
        Console.Clear();
        Console.WriteLine("\n--- CADASTRO DE USUÁRIO ---");
    }

    public string? LerNome() => EntradaConsole.LerOuCancelar("Digite o Nome", s => !string.IsNullOrWhiteSpace(s), "Nome inválido.");

    public string? LerEmail() => EntradaConsole.LerOuCancelar("Digite o Email", s => Regex.IsMatch(s, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"), "Email inválido.");

    public string? LerCpf() => EntradaConsole.LerOuCancelar("Digite o CPF", s => s.Length == 11 && s.All(char.IsDigit), "CPF inválido. Digite apenas os 11 números.");

    public string? LerSenha() => EntradaConsole.LerOuCancelar("Digite a Senha", s => s.Length >= 6, "A senha deve ter pelo menos 6 caracteres.");

    public string? LerOpcaoPlano()
    {
        Console.WriteLine("Selecione o plano:");
        Console.WriteLine("1. BASICO");
        Console.WriteLine("2. PADRAO");
        Console.WriteLine("3. PREMIUM");
        return EntradaConsole.LerOuCancelar("Opção", s => int.TryParse(s, out int op) && op >= 1 && op <= 3, "Plano inválido.");
    }

    public void CadastroCancelado() => Console.WriteLine("Cadastro cancelado.");

    public void PlanoNaoEncontrado(string nomePlano) => Console.WriteLine($"Plano {nomePlano} não está cadastrado no banco.");

    public void CadastroCancelado2FAObrigatorio() => Console.WriteLine("Cadastro cancelado: o 2FA é obrigatório.");

    public void CadastroSucesso() => Console.WriteLine("Usuário registrado com sucesso!");

    public void CadastroErro(string mensagem) => Console.WriteLine("Erro ao registrar: " + mensagem);

    public string? LerEmailLogin() => EntradaConsole.LerOuCancelar("Digite o email", s => Regex.IsMatch(s, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"), "Email inválido.");

    public string? LerSenhaLogin() => EntradaConsole.LerOuCancelar("Digite a senha", s => !string.IsNullOrWhiteSpace(s), "Senha inválida.");

    public void CredenciaisInvalidas() => Console.WriteLine("Email ou senha incorretos.");

    public void ContaBloqueadaPermanente() => Console.WriteLine("Esta conta está bloqueada. Fale com o administrador para liberar o acesso.");

    public void ContaBloqueadaTemporaria(TimeSpan restante, DateTime desbloqueiaAs)
    {
        string tempoFormatado = restante.TotalHours >= 1
            ? $"{(int)restante.TotalHours}h{restante.Minutes:D2}min"
            : $"{restante.Minutes}min";

        Console.WriteLine($"Conta temporariamente bloqueada por excesso de tentativas incorretas. " +
                           $"Tente novamente em {tempoFormatado} (a partir das {desbloqueiaAs:HH:mm}).");
    }

    public void RestamTentativas(int restantes) => Console.WriteLine($"Email ou senha incorretos. Restam {restantes} tentativa(s) antes do bloqueio.");

    public void LimiteTentativasAtingido(int minutos) => Console.WriteLine($"Limite de tentativas atingido. Sua conta foi bloqueada por {minutos} minutos.");

    public void LoginCancelado2FAObrigatorio() => Console.WriteLine("Login cancelado: o 2FA é obrigatório.");

    public string LerCodigo2FA()
    {
        Console.Write("Digite o código de autenticação (2FA): ");
        return (Console.ReadLine() ?? "").Replace(" ", "");
    }

    public void Codigo2FAIncorreto(int restantes) => Console.WriteLine(restantes > 0
        ? $"Código incorreto. Restam {restantes} tentativa(s)."
        : "Código incorreto.");

    public void LoginErro(string mensagem) => Console.WriteLine("Erro ao fazer login: " + mensagem);

    public void BemVindo(string? nomeUsuario) => Console.WriteLine($"Bem-vindo, {nomeUsuario}!");
}
