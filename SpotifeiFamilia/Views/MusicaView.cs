using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Views;

public class MusicaView
{
    public void ExibirMusicas(List<Musica> musicas)
    {
        Console.WriteLine("\n--- MÚSICAS ---");
        foreach (var m in musicas)
            Console.WriteLine($"{m.Id} - {m.Titulo}");
    }

    public string LerTermoBusca()
    {
        Console.Write("Digite o nome da música: ");
        return Console.ReadLine() ?? "";
    }

    public void ExibirResultadosBusca(List<Musica> musicas)
    {
        Console.WriteLine("\n--- RESULTADOS ---");
        if (musicas.Count == 0)
        {
            Console.WriteLine("Nenhuma música encontrada.");
            return;
        }
        foreach (var m in musicas)
            Console.WriteLine($"{m.Id} - {m.Titulo}");
    }

    public int? LerIdMusica()
    {
        Console.Write("Digite o ID da música: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("ID de música inválido.");
            return null;
        }
        return id;
    }

    public void LimiteDiarioAtingido(int limite) => Console.WriteLine($"Limite diário de {limite} músicas atingido. Não é possível reproduzir mais músicas hoje.");

    public void MusicaNaoEncontrada() => Console.WriteLine("Música não encontrada ou bloqueada para esta conta.");

    public void Reproduzindo(string titulo) => Console.WriteLine($"Reproduzindo: {titulo}");

    public void Erro(string acao, string mensagem) => Console.WriteLine($"Erro ao {acao}: {mensagem}");
}
