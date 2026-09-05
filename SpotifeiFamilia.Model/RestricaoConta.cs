namespace SpotifeiFamilia.Model;

public enum TipoRestricao
{
    ARTISTA,
    EXPLICITO_GERAL
}

public class RestricaoConta
{
    public int Id { get; set; }
    public int ContaFilhaId { get; set; }
    public TipoRestricao Tipo { get; set; }
    public int? ArtistaId { get; set; }
}
