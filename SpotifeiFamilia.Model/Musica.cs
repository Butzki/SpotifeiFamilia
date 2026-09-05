namespace SpotifeiFamilia.Model;

public class Musica
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public int? AlbumId { get; set; }
    public bool Explicito { get; set; }
}
