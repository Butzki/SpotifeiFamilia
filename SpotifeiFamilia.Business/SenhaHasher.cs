using System.Security.Cryptography;
using System.Text;

namespace SpotifeiFamilia.Business;

/// <summary>
/// Hash de senha com PBKDF2-HMAC-SHA256 (só BCL, sem pacote extra).
/// Formato armazenado em users.senha:  PBKDF2-SHA256$iterações$saltBase64$hashBase64
/// As iterações ficam no próprio texto, então dá pra aumentá-las no futuro sem
/// invalidar hashes antigos (PrecisaRehash indica quando regravar).
/// </summary>
public static class SenhaHasher
{
    private const string PREFIXO = "PBKDF2-SHA256";
    private const int ITERACOES = 600_000;   // recomendação OWASP para PBKDF2-HMAC-SHA256
    private const int TAMANHO_SALT = 16;
    private const int TAMANHO_HASH = 32;
    private const int ITERACOES_MIN = 100_000;     // limites de sanidade ao LER um hash do banco
    private const int ITERACOES_MAX = 10_000_000;

    public static string Hash(string senha)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(TAMANHO_SALT);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, ITERACOES, HashAlgorithmName.SHA256, TAMANHO_HASH);
        return $"{PREFIXO}${ITERACOES}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>True se o valor gravado já está no formato de hash (e não é senha legada em texto puro).</summary>
    public static bool EhHash(string? armazenado) =>
        armazenado != null && armazenado.StartsWith(PREFIXO + "$", StringComparison.Ordinal);

    public static bool Verificar(string senha, string? armazenado)
    {
        if (!TentarLer(armazenado, out int iteracoes, out byte[] salt, out byte[] esperado))
            return false;

        byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }

    /// <summary>True se o hash foi gerado com menos iterações que o padrão atual.</summary>
    public static bool PrecisaRehash(string armazenado) =>
        TentarLer(armazenado, out int iteracoes, out _, out _) && iteracoes < ITERACOES;

    /// <summary>Comparação em tempo constante, usada só para contas legadas com senha em texto puro.</summary>
    public static bool TextoIgual(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private static bool TentarLer(string? armazenado, out int iteracoes, out byte[] salt, out byte[] hash)
    {
        iteracoes = 0;
        salt = hash = Array.Empty<byte>();

        if (!EhHash(armazenado)) return false;

        string[] partes = armazenado!.Split('$');
        if (partes.Length != 4) return false;
        if (!int.TryParse(partes[1], out iteracoes) || iteracoes < ITERACOES_MIN || iteracoes > ITERACOES_MAX) return false;

        try
        {
            salt = Convert.FromBase64String(partes[2]);
            hash = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length >= 8 && hash.Length >= 16;
    }
}
