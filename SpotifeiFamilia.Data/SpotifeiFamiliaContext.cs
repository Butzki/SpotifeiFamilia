using Microsoft.EntityFrameworkCore;
using SpotifeiFamilia.Model;

namespace SpotifeiFamilia.Data;

public class SpotifeiFamiliaContext : DbContext
{
    private static string connectionString = "Server=127.0.0.1;Database=spotifeio;;Uid=root;Pwd=1234;";

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Plano> Planos { get; set; }
    public DbSet<Album> Albuns { get; set; }
    public DbSet<Artista> Artistas { get; set; }
    public DbSet<Musica> Musicas { get; set; }
    public DbSet<Playlist> Playlists { get; set; }
    public DbSet<PlaylistMusica> PlaylistMusicas { get; set; }
    public DbSet<MusicaArtista> MusicaArtistas { get; set; }
    public DbSet<RestricaoConta> RestricoesConta { get; set; }
    public DbSet<HistoricoReproducao> HistoricosReproducao { get; set; }
    public DbSet<ContagemDiariaReproducao> ContagensDiariasReproducao { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }

    // O schema (bd.sql) já existe e é a fonte da verdade: aqui só mapeamos os
    // tipos do C# para as tabelas/colunas snake_case já criadas, sem migrations do EF.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("users");
            entity.Property(u => u.Id).HasColumnName("id_user");
            entity.Property(u => u.NomeUsuario).HasColumnName("nome_usuario");
            entity.Property(u => u.Cpf).HasColumnName("CPF");
            entity.Property(u => u.Email).HasColumnName("e_mail");
            entity.Property(u => u.Senha).HasColumnName("senha");
            entity.Property(u => u.TotpSecret).HasColumnName("totp_secret");
            entity.Property(u => u.TentativasLogin).HasColumnName("tentativas_login");
            entity.Property(u => u.Bloqueado).HasColumnName("bloqueado");
            entity.Property(u => u.BloqueadoAte).HasColumnName("bloqueado_ate");
            entity.Property(u => u.PlanoId).HasColumnName("plano_id");
            entity.Property(u => u.ResponsavelId).HasColumnName("responsavel_id");
            entity.Property(u => u.LimiteDiarioReproducoes).HasColumnName("limite_diario_reproducoes");

            entity.HasOne<Usuario>()
                  .WithMany()
                  .HasForeignKey(u => u.ResponsavelId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Plano>()
                  .WithMany()
                  .HasForeignKey(u => u.PlanoId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Plano>(entity =>
        {
            entity.ToTable("plano");
            entity.Property(p => p.Id).HasColumnName("id_plano");
            entity.Property(p => p.NomePlanoEnum).HasColumnName("nome_plano").HasConversion<string>();
            entity.Property(p => p.PrecoMensal).HasColumnName("preco_mensal");
        });

        modelBuilder.Entity<Album>(entity =>
        {
            entity.ToTable("albums");
            entity.Property(a => a.Id).HasColumnName("id_album");
            entity.Property(a => a.Titulo).HasColumnName("titulo");
        });

        modelBuilder.Entity<Artista>(entity =>
        {
            entity.ToTable("artists");
            entity.Property(a => a.Id).HasColumnName("id_artist");
            entity.Property(a => a.NomeArtista).HasColumnName("nome_artista");
            entity.Property(a => a.Estilo).HasColumnName("estilo");
            entity.Property(a => a.Bio).HasColumnName("bio");
            entity.Property(a => a.Imagem).HasColumnName("imagem");
        });

        modelBuilder.Entity<Musica>(entity =>
        {
            entity.ToTable("tracks");
            entity.Property(m => m.Id).HasColumnName("id_track");
            entity.Property(m => m.Titulo).HasColumnName("titulo");
            entity.Property(m => m.AlbumId).HasColumnName("album_id");
            entity.Property(m => m.Explicito).HasColumnName("explicito");

            entity.HasOne<Album>()
                  .WithMany()
                  .HasForeignKey(m => m.AlbumId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Playlist>(entity =>
        {
            entity.ToTable("playlists");
            entity.Property(p => p.Id).HasColumnName("id_playlist");
            entity.Property(p => p.UsuarioId).HasColumnName("user_id");
            entity.Property(p => p.NomePlaylist).HasColumnName("nome_playlist");

            entity.HasOne<Usuario>()
                  .WithMany()
                  .HasForeignKey(p => p.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlaylistMusica>(entity =>
        {
            entity.ToTable("playlist_tracks");
            entity.Property(pt => pt.Id).HasColumnName("id");
            entity.Property(pt => pt.PlaylistId).HasColumnName("playlist_id");
            entity.Property(pt => pt.MusicaId).HasColumnName("track_id");

            entity.HasOne<Playlist>()
                  .WithMany()
                  .HasForeignKey(pt => pt.PlaylistId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Musica>()
                  .WithMany()
                  .HasForeignKey(pt => pt.MusicaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MusicaArtista>(entity =>
        {
            entity.ToTable("track_artists");
            entity.HasKey(ta => new { ta.MusicaId, ta.ArtistaId });
            entity.Property(ta => ta.MusicaId).HasColumnName("track_id");
            entity.Property(ta => ta.ArtistaId).HasColumnName("artist_id");
            entity.Property(ta => ta.Papel).HasColumnName("papel").HasConversion<string>();

            entity.HasOne<Musica>()
                  .WithMany()
                  .HasForeignKey(ta => ta.MusicaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Artista>()
                  .WithMany()
                  .HasForeignKey(ta => ta.ArtistaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RestricaoConta>(entity =>
        {
            entity.ToTable("restricoes_conta");
            entity.Property(r => r.Id).HasColumnName("id");
            entity.Property(r => r.ContaFilhaId).HasColumnName("conta_filha_id");
            entity.Property(r => r.Tipo).HasColumnName("tipo").HasConversion<string>();
            entity.Property(r => r.ArtistaId).HasColumnName("artist_id");

            entity.HasOne<Usuario>()
                  .WithMany()
                  .HasForeignKey(r => r.ContaFilhaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Artista>()
                  .WithMany()
                  .HasForeignKey(r => r.ArtistaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HistoricoReproducao>(entity =>
        {
            entity.ToTable("historico_reproducao");
            entity.Property(h => h.Id).HasColumnName("id");
            entity.Property(h => h.UsuarioId).HasColumnName("user_id");
            entity.Property(h => h.MusicaId).HasColumnName("track_id");
            entity.Property(h => h.DataHora).HasColumnName("data_hora");

            entity.HasOne<Usuario>()
                  .WithMany()
                  .HasForeignKey(h => h.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Musica>()
                  .WithMany()
                  .HasForeignKey(h => h.MusicaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContagemDiariaReproducao>(entity =>
        {
            entity.ToTable("contagem_diaria_reproducoes");
            entity.HasKey(c => new { c.UsuarioId, c.Data });
            entity.Property(c => c.UsuarioId).HasColumnName("user_id");
            entity.Property(c => c.Data).HasColumnName("data");
            entity.Property(c => c.Quantidade).HasColumnName("quantidade");

            entity.HasOne<Usuario>()
                  .WithMany()
                  .HasForeignKey(c => c.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
