namespace SpotifeiFamilia.Model;

public enum NomePlano
{
    BASICO,
    PADRAO,
    PREMIUM
}

public class Plano
{
    public int Id { get; set; }
    public NomePlano NomePlanoEnum { get; set; }
    public decimal? PrecoMensal { get; set; }
}
