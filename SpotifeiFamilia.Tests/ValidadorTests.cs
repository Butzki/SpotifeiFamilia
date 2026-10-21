using SpotifeiFamilia.Business;
using Xunit;

namespace SpotifeiFamilia.Tests;

public class ValidadorTests
{
    [Theory]
    [InlineData("Ana Maria")]
    [InlineData("José")]
    [InlineData("O'Brien")]
    public void TryNormalizarNome_aceita_nomes_validos(string entrada)
    {
        Assert.True(Validador.TryNormalizarNome(entrada, out _, out string erro));
        Assert.Empty(erro);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("12345")]
    public void TryNormalizarNome_rejeita_nomes_invalidos(string entrada)
    {
        Assert.False(Validador.TryNormalizarNome(entrada, out _, out string erro));
        Assert.NotEmpty(erro);
    }

    [Fact]
    public void NormalizarNome_colapsa_espacos_duplicados()
    {
        string nome = Validador.NormalizarNome("Ana   Maria");
        Assert.Equal("Ana Maria", nome);
    }

    [Theory]
    [InlineData("usuario@dominio.com")]
    [InlineData("nome.sobrenome@empresa.com.br")]
    public void TryNormalizarEmail_aceita_emails_validos(string entrada)
    {
        Assert.True(Validador.TryNormalizarEmail(entrada, out string email, out _));
        Assert.Equal(entrada.ToLowerInvariant(), email);
    }

    [Theory]
    [InlineData("sem-arroba.com")]
    [InlineData("dois@@arrobas.com")]
    [InlineData("termina.em.ponto@dominio.")]
    [InlineData("")]
    public void TryNormalizarEmail_rejeita_emails_invalidos(string entrada)
    {
        Assert.False(Validador.TryNormalizarEmail(entrada, out _, out string erro));
        Assert.NotEmpty(erro);
    }

    [Theory]
    [InlineData("52998224725")]      // CPF válido conhecido
    [InlineData("529.982.247-25")]   // mesmo CPF, com máscara
    public void TryNormalizarCpf_aceita_cpf_valido_com_ou_sem_mascara(string entrada)
    {
        Assert.True(Validador.TryNormalizarCpf(entrada, out string cpf, out _));
        Assert.Equal("52998224725", cpf);
    }

    [Theory]
    [InlineData("11111111111")]  // todos os dígitos iguais
    [InlineData("12345678900")]  // dígito verificador errado
    [InlineData("123")]          // tamanho errado
    public void TryNormalizarCpf_rejeita_cpf_invalido(string entrada)
    {
        Assert.False(Validador.TryNormalizarCpf(entrada, out _, out string erro));
        Assert.NotEmpty(erro);
    }

    [Theory]
    [InlineData("MinhaS3nhaForte")]
    [InlineData("Outra9Valida")]
    public void TryValidarSenha_aceita_senha_forte(string senha)
    {
        Assert.True(Validador.TryValidarSenha(senha, out string erro));
        Assert.Empty(erro);
    }

    [Theory]
    [InlineData("curta1A")]        // menor que o mínimo
    [InlineData("semnumeromaiuscula")] // sem maiúscula nem número
    [InlineData("senha123")]       // senha comum da lista negada
    [InlineData("")]
    public void TryValidarSenha_rejeita_senha_fraca(string senha)
    {
        Assert.False(Validador.TryValidarSenha(senha, out string erro));
        Assert.NotEmpty(erro);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidarId_lanca_para_id_invalido(int id)
    {
        Assert.Throws<RegraNegocioException>(() => Validador.ValidarId(id, "Usuário"));
    }

    [Fact]
    public void ValidarId_nao_lanca_para_id_valido()
    {
        var exception = Record.Exception(() => Validador.ValidarId(1, "Usuário"));
        Assert.Null(exception);
    }
}
