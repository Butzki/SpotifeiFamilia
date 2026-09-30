using System.Net.Mail;
using System.Text.RegularExpressions;

namespace SpotifeiFamilia.Business;

/// <summary>
/// Regras de formato de cada campo cadastrado. Fonte única da verdade: as Views usam
/// os métodos Try* para validar na digitação e o Business usa os métodos que lançam
/// <see cref="RegraNegocioException"/> antes de tocar no banco. Os limites de tamanho
/// espelham as colunas do bd.sql.
/// </summary>
public static class Validador
{
    public const int NOME_MAX = 50;           // users.nome_usuario VARCHAR(50)
    public const int EMAIL_MAX = 100;         // users.e_mail VARCHAR(100)
    public const int SENHA_MIN = 8;
    public const int SENHA_MAX = 128;
    public const int NOME_PLAYLIST_MAX = 100; // playlists.nome_playlist VARCHAR(100)
    public const int BUSCA_MAX = 100;
    public const int NOME_ARTISTA_MAX = 50;   // artists.nome_artista VARCHAR(50)

    private static readonly Regex Espacos = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex NomePessoa = new(@"^\p{L}[\p{L}\p{M}'’.\- ]*$", RegexOptions.Compiled);
    private static readonly Regex EmailBasico = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    // Senhas que passariam nas regras de composição, mas estão no topo de qualquer lista de vazamentos.
    private static readonly HashSet<string> SenhasComuns = new(StringComparer.Ordinal)
    {
        "password1", "password123", "senha123", "senha1234", "senha@123", "qwerty123",
        "abcd1234", "abc12345", "admin123", "spotify123", "spotifei123", "12345678a", "a1234567"
    };

    // ---------------------------------------------------------------- nome

    public static bool TryNormalizarNome(string? entrada, out string nome, out string erro)
    {
        nome = Espacos.Replace((entrada ?? "").Trim(), " ");
        erro = "";

        if (nome.Length < 2) { erro = "Informe o nome (mínimo 2 caracteres)."; return false; }
        if (nome.Length > NOME_MAX) { erro = $"O nome deve ter no máximo {NOME_MAX} caracteres."; return false; }
        if (!NomePessoa.IsMatch(nome) || nome.Count(char.IsLetter) < 2)
        {
            erro = "O nome deve conter apenas letras, espaços, apóstrofo, hífen e ponto.";
            return false;
        }
        return true;
    }

    public static string NormalizarNome(string? entrada) =>
        TryNormalizarNome(entrada, out string nome, out string erro) ? nome : throw new RegraNegocioException(erro);

    // -------------------------------------------------------------- e-mail

    public static bool TryNormalizarEmail(string? entrada, out string email, out string erro)
    {
        email = (entrada ?? "").Trim().ToLowerInvariant();
        erro = "E-mail inválido.";

        if (email.Length == 0) { erro = "Informe o e-mail."; return false; }
        if (email.Length > EMAIL_MAX) { erro = $"O e-mail deve ter no máximo {EMAIL_MAX} caracteres."; return false; }
        if (!EmailBasico.IsMatch(email) || email.Any(char.IsControl)) return false;

        int arroba = email.IndexOf('@');
        string local = email[..arroba];
        string dominio = email[(arroba + 1)..];

        if (email.IndexOf('@', arroba + 1) >= 0) return false;
        if (local.Length > 64) return false;
        if (email.Contains("..") || local.StartsWith('.') || local.EndsWith('.')) return false;
        if (dominio.StartsWith('.') || dominio.EndsWith('.') || dominio.StartsWith('-') || dominio.EndsWith('-')) return false;

        try
        {
            // Rejeita formas como "Nome <a@b.com>" que o regex simples deixaria passar.
            if (!new MailAddress(email).Address.Equals(email, StringComparison.OrdinalIgnoreCase)) return false;
        }
        catch (FormatException)
        {
            return false;
        }

        erro = "";
        return true;
    }

    public static string NormalizarEmail(string? entrada) =>
        TryNormalizarEmail(entrada, out string email, out string erro) ? email : throw new RegraNegocioException(erro);

    // ----------------------------------------------------------------- CPF

    /// <summary>Aceita com ou sem máscara (000.000.000-00) e devolve só os 11 dígitos.</summary>
    public static bool TryNormalizarCpf(string? entrada, out string cpf, out string erro)
    {
        cpf = new string((entrada ?? "").Where(c => c is not ('.' or '-' or ' ')).ToArray());
        erro = "CPF inválido. Verifique os 11 números.";

        if (cpf.Length != 11 || !cpf.All(c => c is >= '0' and <= '9')) return false;
        if (cpf.Distinct().Count() == 1) return false; // 111.111.111-11 etc. passam no cálculo, mas não existem

        for (int posicao = 9; posicao <= 10; posicao++)
        {
            int soma = 0;
            for (int i = 0; i < posicao; i++)
                soma += (cpf[i] - '0') * (posicao + 1 - i);

            int digitoEsperado = soma * 10 % 11 % 10;
            if (cpf[posicao] - '0' != digitoEsperado) return false;
        }

        erro = "";
        return true;
    }

    public static string NormalizarCpf(string? entrada) =>
        TryNormalizarCpf(entrada, out string cpf, out string erro) ? cpf : throw new RegraNegocioException(erro);

    // --------------------------------------------------------------- senha

    public static bool TryValidarSenha(string? senha, out string erro)
    {
        erro = "";

        if (string.IsNullOrWhiteSpace(senha)) { erro = "Informe a senha."; return false; }

        if (senha.Length < SENHA_MIN || senha.Length > SENHA_MAX)
        {
            erro = $"A senha deve ter entre {SENHA_MIN} e {SENHA_MAX} caracteres.";
            return false;
        }

        if (senha.Any(char.IsControl)) { erro = "A senha contém caracteres inválidos."; return false; }

        if (!senha.Any(char.IsUpper) || !senha.Any(char.IsLower) || !senha.Any(char.IsDigit))
        {
            erro = "A senha deve ter letra maiúscula, letra minúscula e número.";
            return false;
        }

        if (SenhasComuns.Contains(senha.ToLowerInvariant()))
        {
            erro = "Essa senha é muito comum. Escolha outra.";
            return false;
        }

        return true;
    }

    public static void ValidarSenha(string? senha)
    {
        if (!TryValidarSenha(senha, out string erro))
            throw new RegraNegocioException(erro);
    }

    // ------------------------------------------------ textos livres / ids

    public static string NormalizarNomePlaylist(string? entrada) =>
        NormalizarTexto(entrada, "O nome da playlist", NOME_PLAYLIST_MAX);

    public static string NormalizarTermoBusca(string? entrada) =>
        NormalizarTexto(entrada, "O termo de busca", BUSCA_MAX);

    public static string NormalizarNomeArtista(string? entrada) =>
        NormalizarTexto(entrada, "O nome do artista", NOME_ARTISTA_MAX);

    public static void ValidarId(int id, string campo)
    {
        if (id <= 0)
            throw new RegraNegocioException($"{campo} inválido.");
    }

    private static string NormalizarTexto(string? entrada, string campo, int max)
    {
        string texto = Espacos.Replace((entrada ?? "").Trim(), " ");

        if (texto.Length == 0) throw new RegraNegocioException($"{campo} não pode ficar vazio.");
        if (texto.Length > max) throw new RegraNegocioException($"{campo} deve ter no máximo {max} caracteres.");
        if (texto.Any(char.IsControl)) throw new RegraNegocioException($"{campo} contém caracteres inválidos.");

        return texto;
    }
}
