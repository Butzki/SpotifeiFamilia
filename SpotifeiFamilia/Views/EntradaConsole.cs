namespace SpotifeiFamilia.Views;

public static class EntradaConsole
{
    // Lê uma linha do console, repetindo o prompt até receber um valor válido.
    // Digitar "cancelar" a qualquer momento interrompe a operação (retorna null),
    // em vez de obrigar o usuário a fechar o programa pra desistir.
    public static string? LerOuCancelar(string prompt, Func<string, bool> valido, string mensagemInvalida)
    {
        while (true)
        {
            Console.Write($"{prompt} (ou 'cancelar' para sair): ");
            string valor = Console.ReadLine() ?? "";

            if (valor.Trim().Equals("cancelar", StringComparison.OrdinalIgnoreCase))
                return null;

            if (valido(valor))
                return valor;

            Console.WriteLine(mensagemInvalida);
        }
    }
}
