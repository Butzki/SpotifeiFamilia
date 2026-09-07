using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Views;

public class PlaylistView
{
    public string ExibirMenuPlaylists()
    {
        Console.WriteLine("\n--- MINHAS PLAYLISTS ---");
        Console.WriteLine("1. Ver minhas playlists");
        Console.WriteLine("2. Criar playlist");
        Console.WriteLine("3. Ver músicas de uma playlist");
        Console.WriteLine("4. Adicionar música a uma playlist");
        Console.WriteLine("5. Voltar");
        Console.Write("Escolha: ");
        return Console.ReadLine() ?? "";
    }

    public void OpcaoInvalida() => Console.WriteLine("Opção inválida.");

    public void ExibirPlaylists(List<Playlist> playlists)
    {
        Console.WriteLine("\n--- SUAS PLAYLISTS ---");
        if (playlists.Count == 0)
        {
            Console.WriteLine("Nenhuma playlist encontrada.");
            return;
        }
        foreach (var p in playlists)
            Console.WriteLine($"[{p.Id}] {p.NomePlaylist}");
    }

    public string LerNomePlaylist()
    {
        Console.Write("Nome da playlist: ");
        return Console.ReadLine() ?? "";
    }

    public void PlaylistCriada() => Console.WriteLine("Playlist criada com sucesso!");

    public int? LerIdPlaylist()
    {
        Console.Write("Digite o ID da playlist: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("ID de playlist inválido.");
            return null;
        }
        return id;
    }

    public void PlaylistNaoEncontrada() => Console.WriteLine("Playlist não encontrada ou não pertence a você.");

    public void ExibirMusicasPlaylist(List<Musica> musicas)
    {
        Console.WriteLine("\n--- MÚSICAS DA PLAYLIST ---");
        if (musicas.Count == 0)
        {
            Console.WriteLine("Nenhuma música encontrada nesta playlist.");
            return;
        }
        foreach (var m in musicas)
            Console.WriteLine(m.Titulo);
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

    public void MusicaAdicionadaPlaylist() => Console.WriteLine("Música adicionada à playlist com sucesso!");

    public void Erro(string acao, string mensagem) => Console.WriteLine($"Erro ao {acao}: {mensagem}");
}
