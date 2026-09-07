using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Views;

public class RestricaoView
{
    public string ExibirMenuRestricoes()
    {
        Console.WriteLine("\n--- GERENCIAR RESTRIÇÕES ---");
        Console.WriteLine("1. Ver restrições ativas");
        Console.WriteLine("2. Bloquear artista");
        Console.WriteLine("3. Desbloquear artista");
        Console.WriteLine("4. Bloquear conteúdo explícito");
        Console.WriteLine("5. Desbloquear conteúdo explícito");
        Console.WriteLine("6. Definir limite diário de músicas");
        Console.WriteLine("7. Voltar");
        Console.Write("Escolha: ");
        return Console.ReadLine() ?? "";
    }

    public void OpcaoInvalida() => Console.WriteLine("Opção inválida.");

    public void ExibirRestricoes(List<(RestricaoConta Restricao, string? NomeArtista)> restricoes)
    {
        Console.WriteLine("\n--- RESTRIÇÕES ATIVAS ---");
        if (restricoes.Count == 0)
        {
            Console.WriteLine("Nenhuma restrição ativa.");
            return;
        }
        foreach (var (restricao, nomeArtista) in restricoes)
        {
            if (restricao.Tipo == TipoRestricao.ARTISTA)
                Console.WriteLine($"[{restricao.Id}] Artista bloqueado: {nomeArtista}");
            else
                Console.WriteLine($"[{restricao.Id}] Conteúdo explícito bloqueado (geral)");
        }
    }

    public void SemLimiteDiario() => Console.WriteLine("Limite diário de músicas: sem limite.");

    public void ExibirLimiteDiario(int reproduzidasHoje, int limiteDiario)
    {
        string status = reproduzidasHoje >= limiteDiario ? "LIMITE ATINGIDO" : "dentro do limite";
        Console.WriteLine($"Limite diário de músicas: {reproduzidasHoje}/{limiteDiario} reproduzidas hoje ({status}).");
    }

    public string LerNomeArtista()
    {
        Console.Write("Nome do artista a bloquear: ");
        return Console.ReadLine() ?? "";
    }

    public void ArtistaNaoEncontrado() => Console.WriteLine("Nenhum artista encontrado com esse nome.");

    public void ExibirArtistasEncontrados(List<Artista> artistas)
    {
        Console.WriteLine("Mais de um artista encontrado:");
        foreach (var a in artistas)
            Console.WriteLine($"[{a.Id}] {a.NomeArtista}");
    }

    public int? LerIdArtistaEscolhido()
    {
        Console.Write("Digite o ID do artista desejado: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("ID inválido.");
            return null;
        }
        return id;
    }

    public void IdInvalido() => Console.WriteLine("ID inválido.");

    public void ArtistaJaBloqueado() => Console.WriteLine("Esse artista já está bloqueado para esse membro.");

    public void ArtistaBloqueado() => Console.WriteLine("Artista bloqueado com sucesso!");

    public int? LerIdArtistaDesbloquear()
    {
        Console.Write("ID do artista a desbloquear: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("ID inválido.");
            return null;
        }
        return id;
    }

    public void ArtistaDesbloqueado(bool removido) => Console.WriteLine(removido
        ? "Artista desbloqueado com sucesso."
        : "Não havia bloqueio para esse artista.");

    public void ExplicitoJaBloqueado() => Console.WriteLine("Conteúdo explícito já está bloqueado para esse membro.");

    public void ExplicitoBloqueado() => Console.WriteLine("Conteúdo explícito bloqueado com sucesso!");

    public void ExplicitoDesbloqueado(bool removido) => Console.WriteLine(removido
        ? "Conteúdo explícito desbloqueado com sucesso."
        : "Não havia bloqueio de conteúdo explícito para esse membro.");

    public void ExibirLimiteAtual(int? atual) => Console.WriteLine(atual != null
        ? $"Limite diário atual: {atual} música(s) por dia."
        : "Limite diário atual: sem limite.");

    public int? LerNovoLimite()
    {
        Console.Write("Digite o novo limite diário de músicas (0 para remover o limite): ");
        if (!int.TryParse(Console.ReadLine(), out int novoLimite) || novoLimite < 0)
        {
            Console.WriteLine("Valor inválido.");
            return null;
        }
        return novoLimite;
    }

    public void LimiteAtualizado() => Console.WriteLine("Limite diário atualizado com sucesso!");

    public void Erro(string acao, string mensagem) => Console.WriteLine($"Erro ao {acao}: {mensagem}");
}
