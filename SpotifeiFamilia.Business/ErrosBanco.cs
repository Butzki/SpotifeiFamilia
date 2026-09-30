namespace SpotifeiFamilia.Business;

/// <summary>
/// Reconhece erros de integridade do MySQL sem acoplar o Business ao EF Core/Pomelo:
/// a exceção do provider vem embrulhada (DbUpdateException -> MySqlException), então
/// percorremos as inner exceptions procurando a mensagem padrão do MySQL.
/// </summary>
internal static class ErrosBanco
{
    public static bool ViolacaoDeUnicidade(Exception ex) => Contem(ex, "Duplicate entry");

    public static bool ViolacaoDeReferencia(Exception ex) => Contem(ex, "foreign key constraint fails");

    private static bool Contem(Exception? ex, string trecho)
    {
        for (; ex != null; ex = ex.InnerException)
        {
            if (ex.Message.Contains(trecho, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
