namespace SpotifeiFamilia.Model;

public class Artista
{
    public int Id { get; set; }
    public string NomeArtista { get; set; } = string.Empty;
    public string Estilo { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Imagem { get; set; }
}
