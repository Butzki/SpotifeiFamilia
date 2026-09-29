using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Business;

public static class FamiliaBusiness
{
    public static bool Reautenticar(int usuarioId, string senha) => UsuarioRepository.VerificarSenha(usuarioId, senha);

    public static List<(DateTime DataHora, string Titulo)> ListarHistoricoPorUsuario(int usuarioId)
    {
        return HistoricoRepository.ListarPorUsuario(usuarioId);
    }

    public static bool PodeGerenciarFamilia(int usuarioId, out string motivo)
    {
        motivo = string.Empty;

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

    public static List<Usuario> ListarDependentes(int responsavelId) => FamiliaRepository.ListarDependentes(responsavelId);

    public static void AdicionarDependente(int responsavelId, string nome, string email, string senha)
    {
        FamiliaRepository.AdicionarDependente(responsavelId, nome, email, senha);
    }

    public static bool RemoverDependente(int id, int responsavelId) => FamiliaRepository.RemoverDependente(id, responsavelId);

    public static bool EhDependenteDe(int contaFilhaId, int responsavelId) => FamiliaRepository.EhDependenteDe(contaFilhaId, responsavelId);

    public static List<Artista> BuscarArtistasPorNome(string nome) => FamiliaRepository.BuscarArtistasPorNome(nome);
}
