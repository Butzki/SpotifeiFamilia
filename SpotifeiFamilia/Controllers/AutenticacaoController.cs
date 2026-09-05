using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Model;
using SpotifeiFamilia.Services;
using SpotifeiFamilia.Views;

namespace SpotifeiFamilia.Controllers;

public class AutenticacaoController
{
    private const int MAX_TENTATIVAS = 5;
    private const int TEMPO_BLOQUEIO_MINUTOS = 120; // 2 horas
    private const int MAX_TENTATIVAS_2FA = 3;

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
            int? planoId = PlanoRepository.BuscarIdPorNome(nomePlano);
            if (planoId == null)
            {
                view.PlanoNaoEncontrado(nomePlano.ToString());
                return;
            }

            if (!totp.ConfigurarNovoTotp(email, out string totpSecret))
            {
                view.CadastroCancelado2FAObrigatorio();
                return;
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

            view.CadastroSucesso();
        }
        catch (Exception ex)
        {
            view.CadastroErro(ex.Message);
        }
    }

    // Repete só o login (sem voltar pro menu principal) quando o email/senha
    // estão errados mas a conta ainda não foi bloqueada.
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
                var usuario = UsuarioRepository.BuscarPorEmail(email);
                if (usuario == null)
                {
                    view.CredenciaisInvalidas();
                    return null;
                }

                // Bloqueio manual/permanente feito pelo administrador direto no banco.
                if (usuario.Bloqueado)
                {
                    view.ContaBloqueadaPermanente();
                    return null;
                }

                var agora = DateTime.Now;
                int tentativas = usuario.TentativasLogin;

                // Bloqueio temporário por excesso de tentativas ainda dentro do prazo.
                if (usuario.BloqueadoAte.HasValue && usuario.BloqueadoAte.Value > agora)
                {
                    view.ContaBloqueadaTemporaria(usuario.BloqueadoAte.Value - agora, usuario.BloqueadoAte.Value);
                    return null;
                }

                // Se havia um bloqueio temporário e o prazo já passou, libera a conta antes de seguir.
                if (usuario.BloqueadoAte.HasValue && usuario.BloqueadoAte.Value <= agora)
                {
                    UsuarioRepository.ResetarTentativas(usuario.Id);
                    tentativas = 0;
                }

                // Senha incorreta: incrementa tentativas e, ao atingir o limite, bloqueia.
                if (senha != usuario.Senha)
                {
                    tentativas++;

                    if (tentativas >= MAX_TENTATIVAS)
                    {
                        UsuarioRepository.Bloquear(usuario.Id, TEMPO_BLOQUEIO_MINUTOS);
                        view.LimiteTentativasAtingido(TEMPO_BLOQUEIO_MINUTOS);
                        return null;
                    }

                    UsuarioRepository.IncrementarTentativas(usuario.Id, tentativas);
                    view.RestamTentativas(MAX_TENTATIVAS - tentativas);
                    continue; // deixa tentar de novo sem voltar pro menu principal
                }

                // Senha correta: zera o contador de tentativas e qualquer bloqueio temporário.
                UsuarioRepository.ResetarTentativas(usuario.Id);

                // Usuários cadastrados antes do 2FA existir ainda não têm um segredo salvo:
                // exigimos a configuração agora, no primeiro login.
                if (string.IsNullOrEmpty(usuario.TotpSecret))
                {
                    if (!totp.ConfigurarNovoTotp(email, out string novoSecret))
                    {
                        view.LoginCancelado2FAObrigatorio();
                        return null;
                    }

                    UsuarioRepository.AtualizarTotpSecret(usuario.Id, novoSecret);
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
