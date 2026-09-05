namespace SpotifeiFamilia.Model;

public class Usuario
{
    public int Id { get; set; }
    public string? NomeUsuario { get; set; }
    public string? Cpf { get; set; }
    public string? Email { get; set; }
    public string? Senha { get; set; }
    public string? TotpSecret { get; set; }
    public int TentativasLogin { get; set; }
    public bool Bloqueado { get; set; }
    public DateTime? BloqueadoAte { get; set; }
    public int? PlanoId { get; set; }
    public int? ResponsavelId { get; set; }
    public int? LimiteDiarioReproducoes { get; set; }
}
