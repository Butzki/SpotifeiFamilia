using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Business;

/// <summary>
/// Controle parental. Toda operação recebe o responsável (quem está logado) e a conta filha, e só
/// prossegue se a conta filha for mesmo um membro da família desse responsável.
/// </summary>
public static class RestricaoBusiness
{
    public const int LIMITE_DIARIO_MAXIMO = 1000;

    public static List<(RestricaoConta Restricao, string? NomeArtista)> ListarRestricoes(int responsavelId, int contaFilhaId)
    {
        FamiliaBusiness.ExigirResponsavelDe(responsavelId, contaFilhaId);
        return FamiliaRepository.ListarRestricoes(contaFilhaId);
    }

    public static bool ArtistaJaBloqueado(int responsavelId, int contaFilhaId, int artistaId)
    {
        FamiliaBusiness.ExigirResponsavelDe(responsavelId, contaFilhaId);
        return FamiliaRepository.ArtistaJaBloqueado(contaFilhaId, artistaId);
    }

    public static void BloquearArtista(int responsavelId, int contaFilhaId, int artistaId)
    {
        FamiliaBusiness.ExigirResponsavelDe(responsavelId, contaFilhaId);
        Validador.ValidarId(artistaId, "Artista");

        if (!FamiliaRepository.ArtistaExiste(artistaId))
            throw new RegraNegocioException("Artista não encontrado.");

        if (FamiliaRepository.ArtistaJaBloqueado(contaFilhaId, artistaId))
            throw new RegraNegocioException("Este artista já está bloqueado para este membro.");

        FamiliaRepository.BloquearArtista(contaFilhaId, artistaId);
    }

    public static int DesbloquearArtista(int responsavelId, int contaFilhaId, int artistaId)
    {
        FamiliaBusiness.ExigirResponsavelDe(responsavelId, contaFilhaId);
        return FamiliaRepository.DesbloquearArtista(contaFilhaId, artistaId);
    }

    public static bool ExplicitoJaBloqueado(int responsavelId, int contaFilhaId)
    {
        FamiliaBusiness.ExigirResponsavelDe(responsavelId, contaFilhaId);
        return FamiliaRepository.ExplicitoJaBloqueado(contaFilhaId);
    }

    public static void BloquearExplicito(int responsavelId, int contaFilhaId)
    {
        FamiliaBusiness.ExigirResponsavelDe(responsavelId, contaFilhaId);

        if (FamiliaRepository.ExplicitoJaBloqueado(contaFilhaId))
            throw new RegraNegocioException("O conteúdo explícito já está bloqueado para este membro.");

        FamiliaRepository.BloquearExplicito(contaFilhaId);
    }

    public static int DesbloquearExplicito(int responsavelId, int contaFilhaId)
    {
        FamiliaBusiness.ExigirResponsavelDe(responsavelId, contaFilhaId);
        return FamiliaRepository.DesbloquearExplicito(contaFilhaId);
    }

    /// <summary>
    /// null remove o limite. Um valor precisa estar entre 1 e <see cref="LIMITE_DIARIO_MAXIMO"/>:
    /// 0 ou negativo bloquearia toda reprodução da conta sem que ninguém tenha pedido isso.
    /// </summary>
    public static void DefinirLimiteDiario(int responsavelId, int contaFilhaId, int? limite)
    {
        FamiliaBusiness.ExigirResponsavelDe(responsavelId, contaFilhaId);

        if (limite.HasValue && (limite.Value < 1 || limite.Value > LIMITE_DIARIO_MAXIMO))
            throw new RegraNegocioException($"O limite diário deve ficar entre 1 e {LIMITE_DIARIO_MAXIMO} músicas (ou remova o limite).");

        FamiliaRepository.DefinirLimiteDiario(contaFilhaId, limite);
    }

    public static int? ObterLimiteDiario(int responsavelId, int contaFilhaId)
    {
        FamiliaBusiness.ExigirResponsavelDe(responsavelId, contaFilhaId);
        return MusicaRepository.ObterLimiteDiario(contaFilhaId);
    }

    public static int ContarReproduzidasHoje(int responsavelId, int contaFilhaId)
    {
        FamiliaBusiness.ExigirResponsavelDe(responsavelId, contaFilhaId);
        return MusicaRepository.ContarReproduzidasHoje(contaFilhaId);
    }
}
