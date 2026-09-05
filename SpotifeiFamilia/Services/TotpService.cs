using System.Diagnostics;
using QRCoder;
using TwoFactorAuthNet;

namespace SpotifeiFamilia.Services;

public class TotpService
{
    private static readonly TwoFactorAuth _tfa = new TwoFactorAuth("Spotifei Família");

    // Gera um novo segredo TOTP, exibe um QR code local (sem depender de serviço
    // externo) para o usuário escanear no app autenticador e exige que ele
    // confirme digitando o código atual antes de considerar o 2FA configurado.
    public bool ConfigurarNovoTotp(string label, out string secret)
    {
        secret = _tfa.CreateSecret();
        string otpauthUri = _tfa.GetQrText(label, secret);

        Console.WriteLine("\n--- CONFIGURAÇÃO DO SEGUNDO FATOR DE AUTENTICAÇÃO (2FA) ---");

        string? htmlPath = null;
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(otpauthUri, QRCodeGenerator.ECCLevel.Q);
            var pngQrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = pngQrCode.GetGraphic(10);
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
                // Sem Chrome encontrado: abre com o navegador padrão do sistema
                // (arquivos .html quase sempre têm um navegador associado).
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
}
