namespace SpotifeiFamilia.Views;

public class MenuView
{
    public string ExibirMenuInicial()
    {
        Console.WriteLine("\n--- SISTEMA DE AUTENTICAÇÃO ---");
        Console.WriteLine("1. Cadastrar Usuário");
        Console.WriteLine("2. Fazer Login");
        Console.WriteLine("3. Sair");
        Console.Write("Escolha uma opção: ");
        return Console.ReadLine() ?? "";
    }

    public void OpcaoInvalida() => Console.WriteLine("Opção inválida. Tente novamente.");

    public void Saindo() => Console.WriteLine("Encerrando o sistema...");

    public string ExibirMenuPrincipal()
    {
        Console.WriteLine("\n--- MENU PRINCIPAL ---");
        Console.WriteLine("1. Ver músicas");
        Console.WriteLine("2. Buscar música");
        Console.WriteLine("3. Reproduzir música");
        Console.WriteLine("4. Minhas playlists");
        Console.WriteLine("5. Spotifei Família");
        Console.WriteLine("6. Sair");
        Console.Write("Escolha: ");
        return Console.ReadLine() ?? "";
    }
}
