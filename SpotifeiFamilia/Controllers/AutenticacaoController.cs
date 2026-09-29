using SpotifeiFamilia.Business;
using SpotifeiFamilia.Model;
using SpotifeiFamilia.Service;
using SpotifeiFamilia.Views;

namespace SpotifeiFamilia.Controllers;

public class AutenticacaoController
{
    private const int MAX_TENTATIVAS = AutenticacaoBusiness.MAX_TENTATIVAS;
    private const int TEMPO_BLOQUEIO_MINUTOS = AutenticacaoBusiness.TEMPO_BLOQUEIO_MINUTOS;
    private const int MAX_TENTATIVAS_2FA = AutenticacaoBusiness.MAX_TENTATIVAS_2FA;

    private readonly AutenticacaoView view = new();
    private readonly TotpService totp = new();

    public void RegistrarUsuario()
    {
        view.CabecalhoCadastro();

        string? nome = view.LerNome();
        if (nome == null) { view.CadastroCancelado(); return; }

        string? email = view.LerEmail();
        if (email == null) { view.CadastroCancelado(); return; }

        string? cpf = view.LerCpf();
        if (cpf == null) { view.CadastroCancelado(); return; }

        string? senha = view.LerSenha();
        if (senha == null) { view.CadastroCancelado(); return; }

        string? opcaoPlanoTexto = view.LerOpcaoPlano();
        if (opcaoPlanoTexto == null) { view.CadastroCancelado(); return; }

        NomePlano nomePlano = int.Parse(opcaoPlanoTexto) switch
        {
            1 => NomePlano.BASICO,
            2 => NomePlano.PADRAO,
            _ => NomePlano.PREMIUM
        };

        try
        {
            AutenticacaoBusiness.RegistrarUsuario(nome, email, cpf, senha, nomePlano, totp);
            view.CadastroSucesso();
        }
        catch (InvalidOperationException ex)
        {
            view.PlanoNaoEncontrado(nomePlano.ToString());
            if (ex.Message.Contains("2FA"))
            {
                view.CadastroCancelado2FAObrigatorio();
            }
        }
        catch (Exception ex)
        {
            view.CadastroErro(ex.Message);
        }
    }

    public Usuario? Login()
    {
        while (true)
        {
            string? email = view.LerEmailLogin();
            if (email == null) return null;

            string? senha = view.LerSenhaLogin();
            if (senha == null) return null;

            try
            {
                var usuario = AutenticacaoBusiness.BuscarUsuario(email);
                if (usuario == null)
                {
                    view.CredenciaisInvalidas();
                    return null;
                }

                if (usuario.Bloqueado)
                {
                    view.ContaBloqueadaPermanente();
                    return null;
                }

                var agora = DateTime.Now;
                int tentativas = usuario.TentativasLogin;

                if (usuario.BloqueadoAte.HasValue && usuario.BloqueadoAte.Value > agora)
                {
                    view.ContaBloqueadaTemporaria(usuario.BloqueadoAte.Value - agora, usuario.BloqueadoAte.Value);
                    return null;
                }

                if (usuario.BloqueadoAte.HasValue && usuario.BloqueadoAte.Value <= agora)
                {
                    AutenticacaoBusiness.ResetarTentativas(usuario.Id);
                    tentativas = 0;
                }

                if (!AutenticacaoBusiness.SenhaCorreta(usuario, senha))
                {
                    tentativas++;

                    if (tentativas >= MAX_TENTATIVAS)
                    {
                        AutenticacaoBusiness.BloquearConta(usuario.Id);
                        view.LimiteTentativasAtingido(TEMPO_BLOQUEIO_MINUTOS);
                        return null;
                    }

                    AutenticacaoBusiness.RegistrarFalhaLogin(usuario.Id, tentativas);
                    view.RestamTentativas(MAX_TENTATIVAS - tentativas);
                    continue;
                }

                AutenticacaoBusiness.ResetarTentativas(usuario.Id);

                if (string.IsNullOrEmpty(usuario.TotpSecret))
                {
                    if (!totp.ConfigurarNovoTotp(email, out string novoSecret))
                    {
                        view.LoginCancelado2FAObrigatorio();
                        return null;
                    }

                    AutenticacaoBusiness.AtualizarTotpSecret(usuario.Id, novoSecret);
                }
                else
                {
                    bool codigoValido = false;

                    for (int tentativa2fa = 1; tentativa2fa <= MAX_TENTATIVAS_2FA && !codigoValido; tentativa2fa++)
                    {
                        string codigo = view.LerCodigo2FA();
                        codigoValido = totp.VerificarCodigo(usuario.TotpSecret, codigo);

                        if (!codigoValido)
                            view.Codigo2FAIncorreto(MAX_TENTATIVAS_2FA - tentativa2fa);
                    }

                    if (!codigoValido)
                        return null;
                }

                view.BemVindo(usuario.NomeUsuario);
                return usuario;
            }
            catch (Exception ex)
            {
                view.LoginErro(ex.Message);
                return null;
            }
        }
    }
}
