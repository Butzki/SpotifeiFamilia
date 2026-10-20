using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Model;
using SpotifeiFamilia.Service;
using System.Text.RegularExpressions;

namespace SpotifeiFamilia.Business;

public static class AutenticacaoBusiness
{
    public const int MAX_TENTATIVAS = 5;
    public const int TEMPO_BLOQUEIO_MINUTOS = 120;
    public const int MAX_TENTATIVAS_2FA = 3;

    // Tamanho máximo aceito no login: só evita gastar CPU com entradas absurdas (o hash é PBKDF2).
    private const int SENHA_LOGIN_MAX = 1024;

    // Secret TOTP em Base32 (o TwoFactorAuth.Net gera 16 caracteres por padrão; a coluna aceita até 64).
    private static readonly Regex SecretBase32 = new(@"^[A-Z2-7]{16,64}$", RegexOptions.Compiled);

    // Usada pelos fluxos que não recebem (nem precisam customizar) uma instância própria de
    // TotpService, como a verificação de código no login da WebApi.
    private static readonly TotpService _totp = new();

    /// <summary>
    /// Cadastro de titular (fluxo interativo, usado pelo console). Ordem proposital: primeiro
    /// tudo o que dá pra validar sem I/O, depois unicidade e plano, e só então o 2FA (que abre o
    /// QR code no navegador e espera confirmação digitada) — assim ninguém configura o
    /// autenticador para um cadastro que já estava fadado a falhar.
    /// Erros de dado lançam <see cref="RegraNegocioException"/>. Plano inexistente e 2FA não
    /// confirmado continuam como <see cref="InvalidOperationException"/> (contrato do Controller).
    /// </summary>
    public static void RegistrarUsuario(string nome, string email, string cpf, string senha, NomePlano nomePlano, TotpService totp)
    {
        var (nomeValido, emailValido, cpfValido, planoId) = ValidarCadastro(nome, email, cpf, senha, nomePlano);

        if (!totp.ConfigurarNovoTotp(emailValido, out string totpSecret))
            throw new InvalidOperationException("2FA obrigatório para concluir o cadastro.");

        CadastrarNoBanco(nomeValido, emailValido, cpfValido, senha, planoId, totpSecret);
    }

    /// <summary>
    /// Cadastro via WebApi: não há tela esperando o código de 6 dígitos no mesmo request em que
    /// o QR code é mostrado, então a chave já é gerada e gravada aqui; o cliente confirma que
    /// configurou o autenticador corretamente no primeiro login seguinte, que exige o código certo.
    /// </summary>
    public static (int UsuarioId, string TotpSecret, byte[] QrCodePng) RegistrarUsuarioApi(
        string nome, string email, string cpf, string senha, NomePlano nomePlano, TotpService totp)
    {
        var (nomeValido, emailValido, cpfValido, planoId) = ValidarCadastro(nome, email, cpf, senha, nomePlano);

        var (secret, _, qrCodePng) = totp.GerarSecret(emailValido);
        int usuarioId = CadastrarNoBanco(nomeValido, emailValido, cpfValido, senha, planoId, secret);

        return (usuarioId, secret, qrCodePng);
    }

    private static (string Nome, string Email, string Cpf, int PlanoId) ValidarCadastro(
        string nome, string email, string cpf, string senha, NomePlano nomePlano)
    {
        nome = Validador.NormalizarNome(nome);
        email = Validador.NormalizarEmail(email);
        cpf = Validador.NormalizarCpf(cpf);
        Validador.ValidarSenha(senha);

        if (!Enum.IsDefined(nomePlano))
            throw new RegraNegocioException("Plano inválido.");

        if (UsuarioRepository.EmailJaCadastrado(email))
            throw new RegraNegocioException("Já existe uma conta cadastrada com este e-mail.");

        if (UsuarioRepository.CpfJaCadastrado(cpf))
            throw new RegraNegocioException("Já existe uma conta cadastrada com este CPF.");

        int? planoId = PlanoRepository.BuscarIdPorNome(nomePlano);
        if (planoId == null)
            throw new InvalidOperationException($"Plano {nomePlano} não encontrado.");

        return (nome, email, cpf, planoId.Value);
    }

    private static int CadastrarNoBanco(string nome, string email, string cpf, string senha, int planoId, string totpSecret)
    {
        var usuario = new Usuario
        {
            NomeUsuario = nome,
            Cpf = cpf,
            Email = email,
            Senha = SenhaHasher.Hash(senha),
            TotpSecret = totpSecret,
            PlanoId = planoId
        };

        try
        {
            UsuarioRepository.Cadastrar(usuario);
        }
        catch (Exception ex) when (ErrosBanco.ViolacaoDeUnicidade(ex))
        {
            // Outra pessoa cadastrou o mesmo e-mail/CPF entre a checagem acima e o INSERT;
            // os índices UNIQUE do banco são a garantia final.
            throw new RegraNegocioException("E-mail ou CPF já cadastrado.");
        }

        return usuario.Id;
    }

    public static Usuario? BuscarUsuario(string email)
    {
        // No login só normaliza (trim + minúsculas). A validação estrita de formato é do cadastro:
        // aplicá-la aqui trancaria para fora contas antigas cujo e-mail foi aceito por regras mais frouxas.
        string emailNormalizado = (email ?? "").Trim().ToLowerInvariant();
        if (emailNormalizado.Length == 0 || emailNormalizado.Length > Validador.EMAIL_MAX)
            return null;

        return UsuarioRepository.BuscarPorEmail(emailNormalizado);
    }

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

    /// <summary>
    /// Confere a senha contra o hash gravado. Contas criadas antes do hash (senha em texto puro
    /// na coluna) ainda entram e são migradas para hash no primeiro login bem-sucedido; hashes
    /// com menos iterações que o padrão atual também são regravados.
    /// </summary>
    public static bool SenhaCorreta(Usuario usuario, string senha)
    {
        if (string.IsNullOrEmpty(usuario.Senha) || string.IsNullOrEmpty(senha) || senha.Length > SENHA_LOGIN_MAX)
            return false;

        bool correta;
        bool regravar;

        if (SenhaHasher.EhHash(usuario.Senha))
        {
            correta = SenhaHasher.Verificar(senha, usuario.Senha);
            regravar = correta && SenhaHasher.PrecisaRehash(usuario.Senha);
        }
        else
        {
            correta = SenhaHasher.TextoIgual(senha, usuario.Senha);
            regravar = correta;
        }

        if (regravar)
            TentarRegravarHash(usuario, senha);

        return correta;
    }

    private static void TentarRegravarHash(Usuario usuario, string senha)
    {
        try
        {
            string hash = SenhaHasher.Hash(senha);
            UsuarioRepository.AtualizarSenha(usuario.Id, hash);
            usuario.Senha = hash;
        }
        catch (Exception)
        {
            // Não pode derrubar um login válido. O caso típico é o banco ainda estar com
            // users.senha VARCHAR(8) (atualizar_schema_senha_hash.sql não aplicado): a
            // migração simplesmente é tentada de novo no próximo login.
        }
    }

    /// <summary>
    /// Confere o código TOTP de 6 dígitos contra o secret gravado da conta. Usada pelos fluxos
    /// onde não há uma tela interativa pedindo o código em loop (a WebApi, por exemplo): aqui
    /// é uma verificação única por tentativa de login.
    /// </summary>
    public static bool CodigoTotpValido(Usuario usuario, string? codigo)
    {
        if (string.IsNullOrEmpty(usuario.TotpSecret) || string.IsNullOrWhiteSpace(codigo))
            return false;

        return _totp.VerificarCodigo(usuario.TotpSecret, codigo.Replace(" ", ""));
    }

    /// <summary>
    /// Gera e grava uma chave TOTP nova para uma conta que ainda não tem 2FA configurado (contas
    /// antigas, ou dependentes adicionados pelo painel de família) tentando logar pela WebApi.
    /// Sem uma tela esperando a confirmação como no console, é o login seguinte com o código
    /// correto que confirma a configuração.
    /// </summary>
    public static (string Secret, byte[] QrCodePng) IniciarConfiguracaoTotp(Usuario usuario)
    {
        var (secret, _, qrCodePng) = _totp.GerarSecret(usuario.Email ?? usuario.NomeUsuario ?? "SpotifeiFamilia");
        AtualizarTotpSecret(usuario.Id, secret);
        return (secret, qrCodePng);
    }

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

    /// <summary>
    /// Contabiliza mais uma tentativa de senha incorreta e bloqueia a conta ao estourar
    /// <see cref="MAX_TENTATIVAS"/>. Centraliza a regra usada tanto pelo login da WebApi quanto
    /// pela reautenticação antes do painel de família (<see cref="FamiliaBusiness.Reautenticar"/>).
    /// Lança <see cref="RegraNegocioException"/> quando esta tentativa acabou de bloquear a conta.
    /// </summary>
    public static void ProcessarFalhaLogin(Usuario usuario)
    {
        // BloqueadoAte preenchido aqui (depois de ContaBloqueada já ter zerado um bloqueio
        // expirado no banco) significa "o bloqueio anterior expirou e foi zerado agora".
        int tentativas = (usuario.BloqueadoAte.HasValue ? 0 : usuario.TentativasLogin) + 1;

        if (tentativas >= MAX_TENTATIVAS)
        {
            BloquearConta(usuario.Id);
            throw new RegraNegocioException(
                $"Limite de tentativas atingido. Sua conta foi bloqueada por {TEMPO_BLOQUEIO_MINUTOS} minutos.");
        }

        RegistrarFalhaLogin(usuario.Id, tentativas);
    }

    /// <summary>
    /// Grava o secret do 2FA de uma conta que ainda não tem um (contas antigas, no primeiro login).
    /// Nunca sobrescreve um 2FA já configurado: senão quem descobrisse só a senha poderia trocar
    /// o autenticador e anular o segundo fator.
    /// </summary>
    public static void AtualizarTotpSecret(int usuarioId, string secret)
    {
        Validador.ValidarId(usuarioId, "Usuário");

        if (string.IsNullOrEmpty(secret) || !SecretBase32.IsMatch(secret))
            throw new RegraNegocioException("Chave do 2FA inválida.");

        var usuario = UsuarioRepository.BuscarPorId(usuarioId)
            ?? throw new RegraNegocioException("Usuário não encontrado.");

        if (!string.IsNullOrEmpty(usuario.TotpSecret))
            throw new RegraNegocioException("O 2FA desta conta já está configurado.");

        UsuarioRepository.AtualizarTotpSecret(usuarioId, secret);
    }
}
