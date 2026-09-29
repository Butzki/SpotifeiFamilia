using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Business;

public static class RestricaoBusiness
{
    public static List<(RestricaoConta Restricao, string? NomeArtista)> ListarRestricoes(int contaFilhaId)
    {
        return FamiliaRepository.ListarRestricoes(contaFilhaId);
    }

    public static bool ArtistaJaBloqueado(int contaFilhaId, int artistaId)
    {
        return FamiliaRepository.ArtistaJaBloqueado(contaFilhaId, artistaId);
    }

    public static void BloquearArtista(int contaFilhaId, int artistaId)
    {
        FamiliaRepository.BloquearArtista(contaFilhaId, artistaId);
    }

    public static int DesbloquearArtista(int contaFilhaId, int artistaId)
    {
        return FamiliaRepository.DesbloquearArtista(contaFilhaId, artistaId);
    }

    public static bool ExplicitoJaBloqueado(int contaFilhaId)
    {
        return FamiliaRepository.ExplicitoJaBloqueado(contaFilhaId);
    }

    public static void BloquearExplicito(int contaFilhaId)
    {
        FamiliaRepository.BloquearExplicito(contaFilhaId);
    }

    public static int DesbloquearExplicito(int contaFilhaId)
    {
        return FamiliaRepository.DesbloquearExplicito(contaFilhaId);
    }

    public static void DefinirLimiteDiario(int contaFilhaId, int? limite)
    {
        FamiliaRepository.DefinirLimiteDiario(contaFilhaId, limite);
    }

    public static int? ObterLimiteDiario(int contaFilhaId) => MusicaRepository.ObterLimiteDiario(contaFilhaId);

    public static int ContarReproduzidasHoje(int contaFilhaId) => MusicaRepository.ContarReproduzidasHoje(contaFilhaId);
}
