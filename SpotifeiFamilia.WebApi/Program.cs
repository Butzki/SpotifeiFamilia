using SpotifeiFamilia.Business;
using SpotifeiFamilia.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/musicas/{usuarioId:int}", (int usuarioId) =>
{
    var musicas = MusicaBusiness.Listar(usuarioId);
    return Results.Ok(musicas);
});

app.MapGet("/api/musicas/buscar", (int usuarioId, string termo) =>
{
    var musicas = MusicaBusiness.Buscar(usuarioId, termo);
    return Results.Ok(musicas);
});

app.MapPost("/api/autenticacao/login", (LoginRequest request) =>
{
    var usuario = AutenticacaoBusiness.BuscarUsuario(request.Email);
    if (usuario == null)
        return Results.Unauthorized();

    if (usuario.Bloqueado || AutenticacaoBusiness.ContaBloqueada(usuario, out _))
        return Results.Problem("Conta bloqueada ou temporariamente indisponível.", statusCode: StatusCodes.Status423Locked);

    if (!AutenticacaoBusiness.SenhaCorreta(usuario, request.Senha))
        return Results.Unauthorized();

    return Results.Ok(new
    {
        usuario.Id,
        usuario.NomeUsuario,
        usuario.Email,
        usuario.PlanoId
    });
});

app.MapGet("/api/familia/{responsavelId:int}/dependentes", (int responsavelId) =>
{
    var dependentes = FamiliaBusiness.ListarDependentes(responsavelId);
    return Results.Ok(dependentes);
});

app.MapGet("/api/playlists/{usuarioId:int}", (int usuarioId) =>
{
    var playlists = PlaylistBusiness.ListarPorUsuario(usuarioId);
    return Results.Ok(playlists);
});

app.MapPost("/api/playlists", (CriarPlaylistRequest request) =>
{
    PlaylistBusiness.Criar(request.UsuarioId, request.Nome);
    return Results.Ok(new { mensagem = "Playlist criada com sucesso." });
});

app.MapPost("/api/musicas/reproduzir", (ReproducaoRequest request) =>
{
    var musica = MusicaBusiness.BuscarPermitidaPorId(request.UsuarioId, request.MusicaId);
    if (musica == null)
        return Results.NotFound();

    MusicaBusiness.RegistrarReproducao(request.UsuarioId, request.MusicaId);
    return Results.Ok(new { mensagem = $"Reproduzindo: {musica.Titulo}" });
});

app.Run();

public record LoginRequest(string Email, string Senha);
public record CriarPlaylistRequest(int UsuarioId, string Nome);
public record ReproducaoRequest(int UsuarioId, int MusicaId);
