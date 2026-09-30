using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Business;

public static class FamiliaBusiness
{
    // Plano família: além do titular, até 5 membros (6 contas no total). Ajuste aqui se a regra mudar.
    public const int MAX_DEPENDENTES = 5;

    /// <summary>
    /// Confirma a senha do titular antes de abrir o painel da família. Tem o mesmo controle de
    /// tentativas do login: sem isso, o painel seria um oráculo de senha sem limite para quem
    /// pegasse a sessão aberta. Devolve false para senha errada; lança <see cref="RegraNegocioException"/>
    /// quando a conta está bloqueada ou acaba de ser bloqueada por excesso de tentativas.
    /// </summary>
    public static bool Reautenticar(int usuarioId, string senha)
    {
        var usuario = UsuarioRepository.BuscarPorId(usuarioId);
        if (usuario == null)
            return false;

        if (AutenticacaoBusiness.ContaBloqueada(usuario, out TimeSpan? restante))
        {
            throw new RegraNegocioException(restante.HasValue
                ? $"Conta temporariamente bloqueada por excesso de tentativas. Tente novamente em {(int)Math.Ceiling(restante.Value.TotalMinutes)} min."
                : "Esta conta está bloqueada. Fale com o administrador para liberar o acesso.");
        }

        if (AutenticacaoBusiness.SenhaCorreta(usuario, senha))
        {
            if (usuario.TentativasLogin > 0)
                AutenticacaoBusiness.ResetarTentativas(usuarioId);
            return true;
        }

        // ContaBloqueada já zerou no banco um bloqueio expirado, mas o objeto em memória ainda
        // traz o contador antigo — BloqueadoAte preenchido aqui significa "expirou e foi zerado".
        int tentativas = (usuario.BloqueadoAte.HasValue ? 0 : usuario.TentativasLogin) + 1;

        if (tentativas >= AutenticacaoBusiness.MAX_TENTATIVAS)
        {
            AutenticacaoBusiness.BloquearConta(usuarioId);
            throw new RegraNegocioException(
                $"Limite de tentativas atingido. Sua conta foi bloqueada por {AutenticacaoBusiness.TEMPO_BLOQUEIO_MINUTOS} minutos.");
        }

        AutenticacaoBusiness.RegistrarFalhaLogin(usuarioId, tentativas);
        return false;
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

    /// <summary>
    /// Trava de autorização usada por toda operação sobre uma conta filha (histórico, restrições,
    /// limite): quem executa precisa ser titular PREMIUM E a conta precisa ser dele. Os Controllers
    /// já filtram isso na tela, mas o Business não pode depender de quem o chama (ex.: WebApi).
    /// </summary>
    public static void ExigirResponsavelDe(int responsavelId, int contaFilhaId)
    {
        Validador.ValidarId(responsavelId, "Responsável");
        Validador.ValidarId(contaFilhaId, "Membro");

        if (!PodeGerenciarFamilia(responsavelId, out string motivo))
            throw new RegraNegocioException(motivo);

        if (!FamiliaRepository.EhDependenteDe(contaFilhaId, responsavelId))
            throw new RegraNegocioException("Esta conta não pertence à sua família.");
    }

    public static List<(DateTime DataHora, string Titulo)> ListarHistoricoDeDependente(int responsavelId, int contaFilhaId)
    {
        ExigirResponsavelDe(responsavelId, contaFilhaId);
        return HistoricoRepository.ListarPorUsuario(contaFilhaId);
    }

    /// <summary>Lista os membros sem senha, CPF, secret do 2FA ou contadores internos — a entidade completa nunca sai desta camada.</summary>
    public static List<Usuario> ListarDependentes(int responsavelId)
    {
        Validador.ValidarId(responsavelId, "Responsável");

        return FamiliaRepository.ListarDependentes(responsavelId)
            .Select(d => new Usuario
            {
                Id = d.Id,
                NomeUsuario = d.NomeUsuario,
                Email = d.Email,
                PlanoId = d.PlanoId,
                ResponsavelId = d.ResponsavelId,
                LimiteDiarioReproducoes = d.LimiteDiarioReproducoes
            })
            .ToList();
    }

    public static void AdicionarDependente(int responsavelId, string nome, string email, string senha)
    {
        Validador.ValidarId(responsavelId, "Responsável");

        if (!PodeGerenciarFamilia(responsavelId, out string motivo))
            throw new RegraNegocioException(motivo);

        nome = Validador.NormalizarNome(nome);
        email = Validador.NormalizarEmail(email);
        Validador.ValidarSenha(senha);

        if (FamiliaRepository.ListarDependentes(responsavelId).Count >= MAX_DEPENDENTES)
            throw new RegraNegocioException($"Limite de {MAX_DEPENDENTES} membros atingido. Remova um membro para adicionar outro.");

        if (UsuarioRepository.EmailJaCadastrado(email))
            throw new RegraNegocioException("Já existe uma conta cadastrada com este e-mail.");

        try
        {
            FamiliaRepository.AdicionarDependente(responsavelId, nome, email, SenhaHasher.Hash(senha));
        }
        catch (Exception ex) when (ErrosBanco.ViolacaoDeUnicidade(ex))
        {
            throw new RegraNegocioException("Já existe uma conta cadastrada com este e-mail.");
        }
    }

    /// <summary>
    /// O próprio repositório só remove quem tem ResponsavelId == responsavelId, então ninguém
    /// remove conta de outra família. Não exige plano PREMIUM: se o titular perder o plano,
    /// ainda precisa conseguir desfazer a família.
    /// </summary>
    public static bool RemoverDependente(int id, int responsavelId)
    {
        Validador.ValidarId(id, "Membro");
        Validador.ValidarId(responsavelId, "Responsável");

        try
        {
            return FamiliaRepository.RemoverDependente(id, responsavelId);
        }
        catch (Exception ex) when (ErrosBanco.ViolacaoDeReferencia(ex))
        {
            // playlists.user_id não tem ON DELETE CASCADE no bd.sql: enquanto o membro tiver
            // playlists, o banco recusa a exclusão (histórico e restrições, esses sim, caem em cascata).
            throw new RegraNegocioException("Não foi possível remover: este membro ainda possui playlists cadastradas.");
        }
    }

    public static bool EhDependenteDe(int contaFilhaId, int responsavelId)
    {
        if (contaFilhaId <= 0 || responsavelId <= 0)
            return false;

        return FamiliaRepository.EhDependenteDe(contaFilhaId, responsavelId);
    }

    public static List<Artista> BuscarArtistasPorNome(string nome)
    {
        return FamiliaRepository.BuscarArtistasPorNome(Validador.NormalizarNomeArtista(nome));
    }
}
