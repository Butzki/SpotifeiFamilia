using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Model;
using SpotifeiFamilia.Service;

namespace SpotifeiFamilia.Business;

public static class AutenticacaoBusiness
{
    public const int MAX_TENTATIVAS = 5;
    public const int TEMPO_BLOQUEIO_MINUTOS = 120;
    public const int MAX_TENTATIVAS_2FA = 3;

    public static void RegistrarUsuario(string nome, string email, string cpf, string senha, NomePlano nomePlano, TotpService totp)
    {
        int? planoId = PlanoRepository.BuscarIdPorNome(nomePlano);
        if (planoId == null)
        {
            throw new InvalidOperationException($"Plano {nomePlano} não encontrado.");
        }

        if (!totp.ConfigurarNovoTotp(email, out string totpSecret))
        {
            throw new InvalidOperationException("2FA obrigatório para concluir o cadastro.");
        }

        UsuarioRepository.Cadastrar(new Usuario
        {
            NomeUsuario = nome,
            Cpf = cpf,
            Email = email,
            Senha = senha,
            TotpSecret = totpSecret,
            PlanoId = planoId
        });
    }

    public static Usuario? BuscarUsuario(string email) => UsuarioRepository.BuscarPorEmail(email);

    public static bool ContaBloqueada(Usuario usuario, out TimeSpan? tempoRestante)
    {
        tempoRestante = null;

        if (usuario.Bloqueado)
        {
            return true;
        }

        if (usuario.BloqueadoAte.HasValue && usuario.BloqueadoAte.Value > DateTime.Now)
        {
            tempoRestante = usuario.BloqueadoAte.Value - DateTime.Now;
            return true;
        }

        if (usuario.BloqueadoAte.HasValue && usuario.BloqueadoAte.Value <= DateTime.Now)
        {
            UsuarioRepository.ResetarTentativas(usuario.Id);
        }

        return false;
    }

    public static bool SenhaCorreta(Usuario usuario, string senha) => senha == usuario.Senha;

    public static void RegistrarFalhaLogin(int usuarioId, int tentativas)
    {
        UsuarioRepository.IncrementarTentativas(usuarioId, tentativas);
    }

    public static void BloquearConta(int usuarioId)
    {
        UsuarioRepository.Bloquear(usuarioId, TEMPO_BLOQUEIO_MINUTOS);
    }

    public static void ResetarTentativas(int usuarioId)
    {
        UsuarioRepository.ResetarTentativas(usuarioId);
    }

    public static void AtualizarTotpSecret(int usuarioId, string secret)
    {
        UsuarioRepository.AtualizarTotpSecret(usuarioId, secret);
    }
}
