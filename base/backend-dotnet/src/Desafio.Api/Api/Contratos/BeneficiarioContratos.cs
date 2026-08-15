using Desafio.Api.Dominio;

namespace Desafio.Api.Api.Contratos
{
    public sealed record BeneficiarioRequestCriacao(string? NomeCompleto, string? Cpf, DateTime? DataNascimento, Guid? PlanoId);

    public sealed record BeneficiarioRequestAtualizacao(string? NomeCompleto, DateTime? DataNascimento, Guid? PlanoId, StatusBeneficiario Status);

    public sealed record BeneficiarioResponse(Guid Id, string NomeCompleto, string Cpf, DateOnly? DataNascimento, StatusBeneficiario Status, Guid PlanoId, DateTime DataCadastro)
    {
        public static BeneficiarioResponse De(Beneficiario beneficiario) =>
            new(beneficiario.Id, beneficiario.NomeCompleto, beneficiario.Cpf, beneficiario.DataNascimento, beneficiario.Status, beneficiario.PlanoId, beneficiario.DataCadastro);
    }

    public sealed record EnvelopePaginado<T>(IReadOnlyList<T> dados, int pagina, int tamanho, int total);
}