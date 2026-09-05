using System.Linq;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Data.Repositories;

public class PlanoRepository
{
    public static int? BuscarIdPorNome(NomePlano nome)
    {
        using var context = new SpotifeiFamiliaContext();
        return context.Planos
            .Where(p => p.NomePlanoEnum == nome)
            .Select(p => (int?)p.Id)
            .FirstOrDefault();
    }

    public static string? BuscarNomePlanoDoUsuario(int usuarioId)
    {
        using var context = new SpotifeiFamiliaContext();
        int? planoId = context.Usuarios
            .Where(u => u.Id == usuarioId)
            .Select(u => u.PlanoId)
            .FirstOrDefault();

        if (planoId == null)
            return null;

        return context.Planos
            .Where(p => p.Id == planoId)
            .Select(p => p.NomePlanoEnum)
            .Cast<NomePlano?>()
            .FirstOrDefault()
            ?.ToString();
    }
}
