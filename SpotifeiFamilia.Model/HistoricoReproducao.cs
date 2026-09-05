namespace SpotifeiFamilia.Model;

public class HistoricoReproducao
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public int MusicaId { get; set; }
    public DateTime DataHora { get; set; }
}
