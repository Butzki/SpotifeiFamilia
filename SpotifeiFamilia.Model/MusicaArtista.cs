namespace SpotifeiFamilia.Model;

public enum PapelArtista
{
    PRINCIPAL,
    FEAT
}

public class MusicaArtista
{
    public int MusicaId { get; set; }
    public int ArtistaId { get; set; }
    public PapelArtista? Papel { get; set; }
}
