using System.Diagnostics;
using QRCoder;
using TwoFactorAuthNet;

namespace SpotifeiFamilia.Service;

public class TotpService
{
    private static readonly TwoFactorAuth _tfa = new TwoFactorAuth("Spotifei Família");

    /// <summary>
    /// Gera um novo segredo TOTP e o QR code (PNG) correspondente, sem nenhuma interação de
    /// console/navegador. Usado tanto pelo fluxo interativo (<see cref="ConfigurarNovoTotp"/>)
    /// quanto pela WebApi, que devolve o QR direto na resposta em vez de abrir um navegador local.
    /// </summary>
    public (string Secret, string OtpAuthUri, byte[] QrCodePng) GerarSecret(string label)
    {
        string secret = _tfa.CreateSecret();
        string otpauthUri = _tfa.GetQrText(label, secret);

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(otpauthUri, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(qrCodeData);
        byte[] qrCodePng = pngQrCode.GetGraphic(10);

        return (secret, otpauthUri, qrCodePng);
    }

    public bool ConfigurarNovoTotp(string label, out string secret)
    {
        var (secretGerado, _, qrBytes) = GerarSecret(label);
        secret = secretGerado;

        Console.WriteLine("\n--- CONFIGURAÇÃO DO SEGUNDO FATOR DE AUTENTICAÇÃO (2FA) ---");

        string? htmlPath = null;
        try
        {
            string qrBase64 = Convert.ToBase64String(qrBytes);

            htmlPath = Path.Combine(Path.GetTempPath(), $"spotifei_2fa_{Guid.NewGuid():N}.html");
            string html = $"""
                <!DOCTYPE html>
                <html><head><title>Spotifei Família - 2FA</title></head>
                <body style="font-family:sans-serif;text-align:center;padding:2rem">
                <h2>Escaneie com seu app autenticador</h2>
                <img src="data:image/png;base64,{qrBase64}" alt="QR code 2FA" />
                <p>Ou adicione manualmente a chave: <b>{secret}</b></p>
                </body></html>
                """;
            File.WriteAllText(htmlPath, html);

            string[] chromePaths =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
            };
            string? chromeExe = chromePaths.FirstOrDefault(File.Exists);

            if (chromeExe != null)
            {
                Process.Start(new ProcessStartInfo(chromeExe, $"\"{htmlPath}\"") { UseShellExecute = true });
            }
            else
            {
                Process.Start(new ProcessStartInfo(htmlPath) { UseShellExecute = true });
            }

            Console.WriteLine("Escaneie o QR code que acabou de abrir no navegador com seu app autenticador (Google Authenticator, Authy, etc.).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Não foi possível abrir o QR code ({ex.Message}). Adicione a conta manualmente com a chave secreta: {secret}");
        }

        Console.Write("Digite o código de 6 dígitos exibido no app para confirmar: ");
        string codigo = (Console.ReadLine() ?? "").Replace(" ", "");

        if (htmlPath != null)
        {
            try { File.Delete(htmlPath); } catch { }
        }

        if (_tfa.VerifyCode(secret, codigo, discrepancy: 1))
        {
            Console.WriteLine("2FA configurado com sucesso!");
            return true;
        }

        Console.WriteLine("Código incorreto. 2FA não configurado.");
        return false;
    }

    public bool VerificarCodigo(string secret, string codigo)
    {
        return _tfa.VerifyCode(secret, codigo, discrepancy: 1);
    }

    /// <summary>
    /// Calcula o código TOTP válido agora para um secret já configurado. Uso restrito a
    /// ferramentas de desenvolvimento/teste local (ex.: <c>SpotifeiFamilia.Tools</c>): quem chama
    /// isso já tem acesso direto ao banco e ao secret, então não é um jeito de burlar o 2FA de
    /// ninguém — só evita digitar o código do autenticador a cada teste manual da API.
    /// </summary>
    public string GerarCodigoAtual(string secret) => _tfa.GetCode(secret);
}
