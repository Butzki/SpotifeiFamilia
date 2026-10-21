using SpotifeiFamilia.Business;
using Xunit;

namespace SpotifeiFamilia.Tests;

public class SenhaHasherTests
{
    [Fact]
    public void Hash_gera_formato_esperado()
    {
        string hash = SenhaHasher.Hash("MinhaSenha123");

        Assert.StartsWith("PBKDF2-SHA256$", hash);
        Assert.Equal(4, hash.Split('$').Length);
        Assert.True(SenhaHasher.EhHash(hash));
    }

    [Fact]
    public void Hash_gera_valores_diferentes_para_a_mesma_senha_por_causa_do_salt()
    {
        string hash1 = SenhaHasher.Hash("MinhaSenha123");
        string hash2 = SenhaHasher.Hash("MinhaSenha123");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verificar_aceita_a_senha_correta()
    {
        string hash = SenhaHasher.Hash("MinhaSenha123");

        Assert.True(SenhaHasher.Verificar("MinhaSenha123", hash));
    }

    [Fact]
    public void Verificar_rejeita_senha_incorreta()
    {
        string hash = SenhaHasher.Hash("MinhaSenha123");

        Assert.False(SenhaHasher.Verificar("SenhaErrada", hash));
    }

    [Fact]
    public void EhHash_retorna_falso_para_senha_legada_em_texto_puro()
    {
        Assert.False(SenhaHasher.EhHash("senha-em-texto-puro"));
    }

    [Fact]
    public void TextoIgual_compara_senhas_legadas_em_texto_puro()
    {
        Assert.True(SenhaHasher.TextoIgual("abc123", "abc123"));
        Assert.False(SenhaHasher.TextoIgual("abc123", "abc124"));
    }

    [Fact]
    public void PrecisaRehash_eh_falso_para_hash_recem_gerado()
    {
        string hash = SenhaHasher.Hash("MinhaSenha123");

        Assert.False(SenhaHasher.PrecisaRehash(hash));
    }
}
