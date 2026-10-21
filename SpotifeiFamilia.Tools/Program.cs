using SpotifeiFamilia.Business;
using SpotifeiFamilia.Data.Repositories;
using SpotifeiFamilia.Service;

// Ferramenta de desenvolvimento/teste local: calcula o código TOTP válido agora para uma conta
// já cadastrada no banco local, usando o secret salvo nela (gerando um novo se a conta ainda não
// tiver 2FA configurado, como dados de seed criados direto via SQL). Não serve para burlar o 2FA
// de ninguém — só evita abrir o app autenticador a cada teste manual da WebApi (testar_api.bat).
// Uso: dotnet run --project SpotifeiFamilia.Tools -- <email>

if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Uso: dotnet run --project SpotifeiFamilia.Tools -- <email>");
    return 1;
}

string email = args[0].Trim().ToLowerInvariant();
var usuario = UsuarioRepository.BuscarPorEmail(email);

if (usuario == null)
{
    Console.Error.WriteLine($"Nenhuma conta encontrada com o e-mail '{email}'.");
    return 1;
}

string? secret = usuario.TotpSecret;

if (string.IsNullOrEmpty(secret))
{
    Console.Error.WriteLine("Conta sem 2FA configurado: gerando e salvando uma chave nova (mesmo comportamento do primeiro login pela API)...");
    (secret, _) = AutenticacaoBusiness.IniciarConfiguracaoTotp(usuario);
}

var totp = new TotpService();
Console.WriteLine(totp.GerarCodigoAtual(secret));
return 0;
