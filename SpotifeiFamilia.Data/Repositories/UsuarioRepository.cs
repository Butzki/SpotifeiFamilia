using System.Linq;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Data.Repositories;

public class UsuarioRepository
{
    public static Usuario? BuscarPorEmail(string email)
    {
        using var context = new SpotifeiFamiliaContext();
        return context.Usuarios.FirstOrDefault(u => u.Email == email);
    }

    public static Usuario? BuscarPorId(int id)
    {
        using var context = new SpotifeiFamiliaContext();
        return context.Usuarios.FirstOrDefault(u => u.Id == id);
    }

    public static bool EmailJaCadastrado(string email)
    {
        using var context = new SpotifeiFamiliaContext();
        return context.Usuarios.Any(u => u.Email == email);
    }

    public static bool CpfJaCadastrado(string cpf)
    {
        using var context = new SpotifeiFamiliaContext();
        return context.Usuarios.Any(u => u.Cpf == cpf);
    }

    public static void AtualizarSenha(int usuarioId, string senhaHash)
    {
        using var context = new SpotifeiFamiliaContext();
        var usuario = context.Usuarios.First(u => u.Id == usuarioId);
        usuario.Senha = senhaHash;
        context.SaveChanges();
    }

    public static void Cadastrar(Usuario usuario)
    {
        using var context = new SpotifeiFamiliaContext();
        context.Usuarios.Add(usuario);
        context.SaveChanges();
    }

    public static void IncrementarTentativas(int usuarioId, int tentativas)
    {
        using var context = new SpotifeiFamiliaContext();
        var usuario = context.Usuarios.First(u => u.Id == usuarioId);
        usuario.TentativasLogin = tentativas;
        context.SaveChanges();
    }

    public static void Bloquear(int usuarioId, int minutos)
    {
        using var context = new SpotifeiFamiliaContext();
        var usuario = context.Usuarios.First(u => u.Id == usuarioId);
        usuario.TentativasLogin = 0;
        usuario.BloqueadoAte = DateTime.Now.AddMinutes(minutos);
        context.SaveChanges();
    }

    public static void ResetarTentativas(int usuarioId)
    {
        using var context = new SpotifeiFamiliaContext();
        var usuario = context.Usuarios.First(u => u.Id == usuarioId);
        usuario.TentativasLogin = 0;
        usuario.BloqueadoAte = null;
        context.SaveChanges();
    }

    public static void AtualizarTotpSecret(int usuarioId, string secret)
    {
        using var context = new SpotifeiFamiliaContext();
        var usuario = context.Usuarios.First(u => u.Id == usuarioId);
        usuario.TotpSecret = secret;
        context.SaveChanges();
    }
}
