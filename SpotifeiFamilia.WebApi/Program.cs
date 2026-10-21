using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using SpotifeiFamilia.Business;
using SpotifeiFamilia.Model;
using SpotifeiFamilia.Service;

var builder = WebApplication.CreateBuilder(args);

string jwtKey = builder.Configuration["Jwt:Key"]!;
string jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
string jwtAudience = builder.Configuration["Jwt:Audience"]!;
int jwtExpiraMinutos = builder.Configuration.GetValue("Jwt:ExpiraMinutos", 480);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Qualquer exceção não tratada vira JSON em vez de estourar como erro 500 cru: regras de
// negócio (RegraNegocioException) viram 400 com a mensagem feita para o usuário final; o
// resto é logado e devolvido como mensagem genérica (sem vazar detalhes internos/stack trace).
app.UseExceptionHandler(tratamento => tratamento.Run(async context =>
{
    var erro = context.Features.Get<IExceptionHandlerFeature>()?.Error;

    if (erro is RegraNegocioException regraNegocio)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { erro = regraNegocio.Message });
        return;
    }

    context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger("ExceptionHandler")
        .LogError(erro, "Erro não tratado na WebApi");

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { erro = "Ocorreu um erro inesperado." });
}));

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

// ------------------------------------------------------------- autenticação

app.MapPost("/api/autenticacao/cadastro", (CadastroRequest request) =>
{
    if (!Enum.TryParse(request.Plano, ignoreCase: true, out NomePlano nomePlano) || !Enum.IsDefined(nomePlano))
        throw new RegraNegocioException("Plano inválido. Use BASICO, PADRAO ou PREMIUM.");

    var totp = new TotpService();
    var (usuarioId, totpSecret, qrCodePng) = AutenticacaoBusiness.RegistrarUsuarioApi(
        request.Nome, request.Email, request.Cpf, request.Senha, nomePlano, totp);

    return Results.Ok(new
    {
        usuarioId,
        totpSecret,
        qrCodeBase64 = Convert.ToBase64String(qrCodePng),
        mensagem = "Cadastro realizado. Escaneie o QR code (ou use a chave manual) no seu app autenticador antes do primeiro login."
    });
});

app.MapPost("/api/autenticacao/login", (LoginRequest request) =>
{
    var usuario = AutenticacaoBusiness.BuscarUsuario(request.Email);
    if (usuario == null)
        return Results.Unauthorized();

    if (usuario.Bloqueado || AutenticacaoBusiness.ContaBloqueada(usuario, out _))
        return Results.Problem("Conta bloqueada ou temporariamente indisponível.", statusCode: StatusCodes.Status423Locked);

    if (!AutenticacaoBusiness.SenhaCorreta(usuario, request.Senha))
    {
        try
        {
            AutenticacaoBusiness.ProcessarFalhaLogin(usuario);
        }
        catch (RegraNegocioException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status423Locked);
        }

        return Results.Unauthorized();
    }

    AutenticacaoBusiness.ResetarTentativas(usuario.Id);

    if (string.IsNullOrEmpty(usuario.TotpSecret))
    {
        var (secret, qrCodePng) = AutenticacaoBusiness.IniciarConfiguracaoTotp(usuario);
        return Results.Ok(new
        {
            precisaConfigurar2fa = true,
            totpSecret = secret,
            qrCodeBase64 = Convert.ToBase64String(qrCodePng),
            mensagem = "Escaneie o QR code (ou use a chave manual) no seu app autenticador e faça login novamente informando o código de 6 dígitos."
        });
    }

    if (!AutenticacaoBusiness.CodigoTotpValido(usuario, request.Codigo2fa))
        return Results.Unauthorized();

    string token = GerarToken(usuario, jwtKey, jwtIssuer, jwtAudience, jwtExpiraMinutos);

    return Results.Ok(new
    {
        token,
        expiraEm = DateTime.UtcNow.AddMinutes(jwtExpiraMinutos),
        usuario.Id,
        usuario.NomeUsuario,
        usuario.Email,
        usuario.PlanoId
    });
});

// ----------------------------------------------------------------- músicas

app.MapGet("/api/musicas/{usuarioId:int}", (int usuarioId, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, usuarioId);
    if (erro != null) return erro;

    var musicas = MusicaBusiness.Listar(usuarioId);
    return Results.Ok(musicas);
}).RequireAuthorization();

app.MapGet("/api/musicas/buscar", (int usuarioId, string termo, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, usuarioId);
    if (erro != null) return erro;

    var musicas = MusicaBusiness.Buscar(usuarioId, termo);
    return Results.Ok(musicas);
}).RequireAuthorization();

app.MapPost("/api/musicas/reproduzir", (ReproducaoRequest request, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, request.UsuarioId);
    if (erro != null) return erro;

    var musica = MusicaBusiness.BuscarPermitidaPorId(request.UsuarioId, request.MusicaId);
    if (musica == null)
        return Results.NotFound();

    MusicaBusiness.RegistrarReproducao(request.UsuarioId, request.MusicaId);
    return Results.Ok(new { mensagem = $"Reproduzindo: {musica.Titulo}" });
}).RequireAuthorization();

// --------------------------------------------------------------- playlists

app.MapGet("/api/playlists/{usuarioId:int}", (int usuarioId, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, usuarioId);
    if (erro != null) return erro;

    var playlists = PlaylistBusiness.ListarPorUsuario(usuarioId);
    return Results.Ok(playlists);
}).RequireAuthorization();

app.MapPost("/api/playlists", (CriarPlaylistRequest request, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, request.UsuarioId);
    if (erro != null) return erro;

    PlaylistBusiness.Criar(request.UsuarioId, request.Nome);
    return Results.Ok(new { mensagem = "Playlist criada com sucesso." });
}).RequireAuthorization();

app.MapGet("/api/playlists/{usuarioId:int}/{playlistId:int}/musicas", (int usuarioId, int playlistId, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, usuarioId);
    if (erro != null) return erro;

    var musicas = MusicaBusiness.MusicasDaPlaylistPermitidas(usuarioId, playlistId);
    return Results.Ok(musicas);
}).RequireAuthorization();

app.MapPost("/api/playlists/{playlistId:int}/musicas", (int playlistId, AdicionarMusicaRequest request, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, request.UsuarioId);
    if (erro != null) return erro;

    PlaylistBusiness.AdicionarMusica(request.UsuarioId, playlistId, request.MusicaId);
    return Results.Ok(new { mensagem = "Música adicionada à playlist." });
}).RequireAuthorization();

// ------------------------------------------------------------------ família

app.MapGet("/api/familia/{responsavelId:int}/dependentes", (int responsavelId, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, responsavelId);
    if (erro != null) return erro;

    var dependentes = FamiliaBusiness.ListarDependentes(responsavelId);
    return Results.Ok(dependentes);
}).RequireAuthorization();

app.MapPost("/api/familia/{responsavelId:int}/dependentes", (int responsavelId, AdicionarDependenteRequest request, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, responsavelId);
    if (erro != null) return erro;

    FamiliaBusiness.AdicionarDependente(responsavelId, request.Nome, request.Email, request.Senha);
    return Results.Ok(new { mensagem = "Membro adicionado à família." });
}).RequireAuthorization();

app.MapDelete("/api/familia/{responsavelId:int}/dependentes/{contaFilhaId:int}", (int responsavelId, int contaFilhaId, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, responsavelId);
    if (erro != null) return erro;

    bool removido = FamiliaBusiness.RemoverDependente(contaFilhaId, responsavelId);
    return removido ? Results.Ok(new { mensagem = "Membro removido da família." }) : Results.NotFound();
}).RequireAuthorization();

app.MapGet("/api/familia/{responsavelId:int}/dependentes/{contaFilhaId:int}/historico", (int responsavelId, int contaFilhaId, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, responsavelId);
    if (erro != null) return erro;

    var historico = FamiliaBusiness.ListarHistoricoDeDependente(responsavelId, contaFilhaId)
        .Select(h => new { h.DataHora, h.Titulo });

    return Results.Ok(historico);
}).RequireAuthorization();

app.MapPost("/api/familia/{responsavelId:int}/reautenticar", (int responsavelId, ReautenticarRequest request, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, responsavelId);
    if (erro != null) return erro;

    bool confirmado = FamiliaBusiness.Reautenticar(responsavelId, request.Senha);
    return confirmado ? Results.Ok(new { confirmado = true }) : Results.Unauthorized();
}).RequireAuthorization();

// --------------------------------------------------------- controle parental

app.MapGet("/api/familia/{responsavelId:int}/dependentes/{contaFilhaId:int}/restricoes", (int responsavelId, int contaFilhaId, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, responsavelId);
    if (erro != null) return erro;

    var restricoes = RestricaoBusiness.ListarRestricoes(responsavelId, contaFilhaId)
        .Select(r => new { r.Restricao.Id, Tipo = r.Restricao.Tipo.ToString(), r.Restricao.ArtistaId, r.NomeArtista });

    return Results.Ok(restricoes);
}).RequireAuthorization();

app.MapPost("/api/familia/{responsavelId:int}/dependentes/{contaFilhaId:int}/restricoes/artistas/{artistaId:int}", (int responsavelId, int contaFilhaId, int artistaId, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, responsavelId);
    if (erro != null) return erro;

    RestricaoBusiness.BloquearArtista(responsavelId, contaFilhaId, artistaId);
    return Results.Ok(new { mensagem = "Artista bloqueado para este membro." });
}).RequireAuthorization();

app.MapDelete("/api/familia/{responsavelId:int}/dependentes/{contaFilhaId:int}/restricoes/artistas/{artistaId:int}", (int responsavelId, int contaFilhaId, int artistaId, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, responsavelId);
    if (erro != null) return erro;

    int linhas = RestricaoBusiness.DesbloquearArtista(responsavelId, contaFilhaId, artistaId);
    return linhas > 0 ? Results.Ok(new { mensagem = "Artista desbloqueado." }) : Results.NotFound();
}).RequireAuthorization();

app.MapPost("/api/familia/{responsavelId:int}/dependentes/{contaFilhaId:int}/restricoes/explicito", (int responsavelId, int contaFilhaId, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, responsavelId);
    if (erro != null) return erro;

    RestricaoBusiness.BloquearExplicito(responsavelId, contaFilhaId);
    return Results.Ok(new { mensagem = "Conteúdo explícito bloqueado para este membro." });
}).RequireAuthorization();

app.MapDelete("/api/familia/{responsavelId:int}/dependentes/{contaFilhaId:int}/restricoes/explicito", (int responsavelId, int contaFilhaId, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, responsavelId);
    if (erro != null) return erro;

    int linhas = RestricaoBusiness.DesbloquearExplicito(responsavelId, contaFilhaId);
    return linhas > 0 ? Results.Ok(new { mensagem = "Conteúdo explícito desbloqueado." }) : Results.NotFound();
}).RequireAuthorization();

app.MapGet("/api/familia/{responsavelId:int}/dependentes/{contaFilhaId:int}/limite-diario", (int responsavelId, int contaFilhaId, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, responsavelId);
    if (erro != null) return erro;

    int? limite = RestricaoBusiness.ObterLimiteDiario(responsavelId, contaFilhaId);
    int reproduzidasHoje = RestricaoBusiness.ContarReproduzidasHoje(responsavelId, contaFilhaId);
    return Results.Ok(new { limiteDiario = limite, reproduzidasHoje });
}).RequireAuthorization();

app.MapPut("/api/familia/{responsavelId:int}/dependentes/{contaFilhaId:int}/limite-diario", (int responsavelId, int contaFilhaId, LimiteDiarioRequest request, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, responsavelId);
    if (erro != null) return erro;

    RestricaoBusiness.DefinirLimiteDiario(responsavelId, contaFilhaId, request.Limite);
    return Results.Ok(new { mensagem = "Limite diário atualizado." });
}).RequireAuthorization();

app.Run();

// Confere se o dono do token (claim NameIdentifier) é o mesmo usuarioId da rota/body.
// Sem isso, qualquer usuário autenticado poderia ler/alterar dados de outro só trocando o id na URL.
static IResult? VerificarDono(ClaimsPrincipal user, int usuarioId)
{
    string? idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (idClaim != null && int.TryParse(idClaim, out int idAutenticado) && idAutenticado == usuarioId)
        return null;

    return Results.Forbid();
}

static string GerarToken(Usuario usuario, string key, string issuer, string audience, int expiraMinutos)
{
    Claim[] claims =
    [
        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new Claim(ClaimTypes.Email, usuario.Email ?? string.Empty),
        new Claim(ClaimTypes.Name, usuario.NomeUsuario ?? string.Empty)
    ];

    var credenciais = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(expiraMinutos),
        signingCredentials: credenciais);

    return new JwtSecurityTokenHandler().WriteToken(token);
}

public record LoginRequest(string Email, string Senha, string? Codigo2fa = null);
public record CadastroRequest(string Nome, string Email, string Cpf, string Senha, string Plano);
public record CriarPlaylistRequest(int UsuarioId, string Nome);
public record ReproducaoRequest(int UsuarioId, int MusicaId);
public record AdicionarMusicaRequest(int UsuarioId, int MusicaId);
public record AdicionarDependenteRequest(string Nome, string Email, string Senha);
public record ReautenticarRequest(string Senha);
public record LimiteDiarioRequest(int? Limite);
