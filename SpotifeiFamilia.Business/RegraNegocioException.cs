namespace SpotifeiFamilia.Business;

/// <summary>
/// Violação de uma regra de negócio (dado inválido, operação não permitida para
/// aquele usuário, limite excedido...). A mensagem é escrita para o usuário final
/// e pode ser exibida como está pela View ou devolvida pela API.
/// </summary>
public class RegraNegocioException : Exception
{
    public RegraNegocioException(string mensagem) : base(mensagem) { }
}
