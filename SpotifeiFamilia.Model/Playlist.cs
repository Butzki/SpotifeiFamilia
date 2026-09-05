namespace SpotifeiFamilia.Model;

public class Playlist
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public string NomePlaylist { get; set; } = string.Empty;
}
