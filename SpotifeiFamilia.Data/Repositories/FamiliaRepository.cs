using System.Collections.Generic;
using System.Linq;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Data.Repositories;

public class FamiliaRepository
{
    public static List<Usuario> ListarDependentes(int responsavelId)
    {
        using var context = new SpotifeiFamiliaContext();
        return context.Usuarios.Where(u => u.ResponsavelId == responsavelId).ToList();
    }

    public static bool EhDependenteDe(int contaFilhaId, int responsavelId)
    {
        using var context = new SpotifeiFamiliaContext();
        return context.Usuarios.Any(u => u.Id == contaFilhaId && u.ResponsavelId == responsavelId);
    }

    public static void AdicionarDependente(int responsavelId, string nome, string email, string senha)
    {
        using var context = new SpotifeiFamiliaContext();
        // CPF e plano ficam NULL: dependente não precisa de CPF nem plano próprio
        // (usa o plano do responsável).
        context.Usuarios.Add(new Usuario
        {
            NomeUsuario = nome,
            Email = email,
            Senha = senha,
            ResponsavelId = responsavelId
        });
        context.SaveChanges();
    }

    public static bool RemoverDependente(int id, int responsavelId)
    {
        using var context = new SpotifeiFamiliaContext();
        var dependente = context.Usuarios.FirstOrDefault(u => u.Id == id && u.ResponsavelId == responsavelId);
        if (dependente == null)
            return false;

        context.Usuarios.Remove(dependente);
        context.SaveChanges();
        return true;
    }

    // Retorna (ehDependente, nomeDoPlano) para a regra "só titular com plano PREMIUM
    // pode gerenciar a família".
    public static (bool EhDependente, string? NomePlano) ObterInfoPermissao(int usuarioId)
    {
        using var context = new SpotifeiFamiliaContext();
        var usuario = context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);
        if (usuario == null)
            return (false, null);

        bool ehDependente = usuario.ResponsavelId != null;
        string? nomePlano = usuario.PlanoId == null
            ? null
            : context.Planos.Where(p => p.Id == usuario.PlanoId).Select(p => p.NomePlanoEnum).Cast<NomePlano?>().FirstOrDefault()?.ToString();

        return (ehDependente, nomePlano);
    }

    public static List<Artista> BuscarArtistasPorNome(string nome)
    {
        using var context = new SpotifeiFamiliaContext();
        return context.Artistas.Where(a => a.NomeArtista.Contains(nome)).ToList();
    }

    public static List<(RestricaoConta Restricao, string? NomeArtista)> ListarRestricoes(int contaFilhaId)
    {
        using var context = new SpotifeiFamiliaContext();
        return (from r in context.RestricoesConta
                where r.ContaFilhaId == contaFilhaId
                join a in context.Artistas on r.ArtistaId equals a.Id into artistas
                from a in artistas.DefaultIfEmpty()
                select new { r, NomeArtista = (string?)a.NomeArtista })
               .AsEnumerable()
               .Select(x => (x.r, (string?)x.NomeArtista))
               .ToList();
    }

    public static bool ArtistaJaBloqueado(int contaFilhaId, int artistaId)
    {
        using var context = new SpotifeiFamiliaContext();
        return context.RestricoesConta.Any(r => r.ContaFilhaId == contaFilhaId && r.Tipo == TipoRestricao.ARTISTA && r.ArtistaId == artistaId);
    }

    public static void BloquearArtista(int contaFilhaId, int artistaId)
    {
        using var context = new SpotifeiFamiliaContext();
        context.RestricoesConta.Add(new RestricaoConta { ContaFilhaId = contaFilhaId, Tipo = TipoRestricao.ARTISTA, ArtistaId = artistaId });
        context.SaveChanges();
    }

    public static int DesbloquearArtista(int contaFilhaId, int artistaId)
    {
        using var context = new SpotifeiFamiliaContext();
        var restricao = context.RestricoesConta.FirstOrDefault(r => r.ContaFilhaId == contaFilhaId && r.Tipo == TipoRestricao.ARTISTA && r.ArtistaId == artistaId);
        if (restricao == null)
            return 0;

        context.RestricoesConta.Remove(restricao);
        return context.SaveChanges();
    }

    public static bool ExplicitoJaBloqueado(int contaFilhaId)
    {
        using var context = new SpotifeiFamiliaContext();
        return context.RestricoesConta.Any(r => r.ContaFilhaId == contaFilhaId && r.Tipo == TipoRestricao.EXPLICITO_GERAL);
    }

    public static void BloquearExplicito(int contaFilhaId)
    {
        using var context = new SpotifeiFamiliaContext();
        context.RestricoesConta.Add(new RestricaoConta { ContaFilhaId = contaFilhaId, Tipo = TipoRestricao.EXPLICITO_GERAL, ArtistaId = null });
        context.SaveChanges();
    }

    public static int DesbloquearExplicito(int contaFilhaId)
    {
        using var context = new SpotifeiFamiliaContext();
        var restricao = context.RestricoesConta.FirstOrDefault(r => r.ContaFilhaId == contaFilhaId && r.Tipo == TipoRestricao.EXPLICITO_GERAL);
        if (restricao == null)
            return 0;

        context.RestricoesConta.Remove(restricao);
        return context.SaveChanges();
    }

    public static void DefinirLimiteDiario(int contaFilhaId, int? limite)
    {
        using var context = new SpotifeiFamiliaContext();
        var usuario = context.Usuarios.First(u => u.Id == contaFilhaId);
        usuario.LimiteDiarioReproducoes = limite;
        context.SaveChanges();
    }
}
