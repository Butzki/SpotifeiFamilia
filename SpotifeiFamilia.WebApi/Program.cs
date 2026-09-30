using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SpotifeiFamilia.Business;
using SpotifeiFamilia.Model;

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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

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

app.MapPost("/api/autenticacao/login", (LoginRequest request) =>
{
    var usuario = AutenticacaoBusiness.BuscarUsuario(request.Email);
    if (usuario == null)
        return Results.Unauthorized();

    if (usuario.Bloqueado || AutenticacaoBusiness.ContaBloqueada(usuario, out _))
        return Results.Problem("Conta bloqueada ou temporariamente indisponível.", statusCode: StatusCodes.Status423Locked);

    if (!AutenticacaoBusiness.SenhaCorreta(usuario, request.Senha))
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

app.MapGet("/api/familia/{responsavelId:int}/dependentes", (int responsavelId, ClaimsPrincipal user) =>
{
    var erro = VerificarDono(user, responsavelId);
    if (erro != null) return erro;

    var dependentes = FamiliaBusiness.ListarDependentes(responsavelId);
    return Results.Ok(dependentes);
}).RequireAuthorization();

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

public record LoginRequest(string Email, string Senha);
public record CriarPlaylistRequest(int UsuarioId, string Nome);
public record ReproducaoRequest(int UsuarioId, int MusicaId);
