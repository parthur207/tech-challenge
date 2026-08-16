using System.Globalization;
using System.Text.RegularExpressions;
namespace Desafio.Api.Dominio;

public enum StatusBeneficiario
{
    ATIVO,
    INATIVO
}

public class Beneficiario
{
    private Beneficiario() { }

    public Beneficiario(string? nomeCompleto, string? cpf, string? dataNascimento, Guid planoId)
    {
        var detalhes = new List<DetalheErro>();

        ValidarCpf(cpf, detalhes);
        ValidarNome(nomeCompleto, detalhes);
        var dataNasc = ValidarDataNascimento(dataNascimento, detalhes);

        if(detalhes.Count > 0)
            throw new ValidacaoException("Dados do beneficiário inválidos", detalhes);

        Id = Guid.NewGuid();
        Status = StatusBeneficiario.ATIVO;
        DataCadastro = DateTime.UtcNow;
        PlanoId = planoId;
        Cpf = cpf!.Trim();
        NomeCompleto = nomeCompleto!.Trim();
        DataCadastro = DateTime.UtcNow;
        DataNascimento = dataNasc!.Value;
    }
    public Guid Id { get; private set; }

    public string NomeCompleto { get; private set; } = null!;

    public string Cpf { get; private set; } = null!;

    public DateOnly? DataNascimento { get; private set; }

    public StatusBeneficiario Status { get; private set; }

    public Guid PlanoId { get; private set; }

    public DateTime DataCadastro { get; private set; }
    public DateTime? ExcluidoEm { get; private set; }

    private void ValidarCpf(string? cpf, List<DetalheErro> detalhes)
    {
        var valor = cpf?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(valor))
        {
            detalhes.Add(new DetalheErro("Cpf", "obrigatorio."));
            return;
        }

        if (!Regex.IsMatch(valor, @"^\d{11}$"))
        {
            detalhes.Add(new DetalheErro("Cpf", "formato_invalido"));
            return;
        }

        if (valor.Distinct().Count() == 1 || !CpfValido(valor))
        {
            detalhes.Add(new DetalheErro("Cpf", "invalido"));
            return;
        }
    }

    private void ValidarNome(string? nomeCompleto, List<DetalheErro> detalhes)
    {
        var valor = nomeCompleto?.TrimStart().TrimEnd() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(valor))
        {
            detalhes.Add(new DetalheErro("NomeCompleto", "obrigatorio"));
            return;
        }
        if (valor.Length < 2 || valor.Length > 120)
        {
            detalhes.Add(new DetalheErro("NomeCompleto", "tamanho_invalido"));
            return;
        }
    }

    private DateOnly? ValidarDataNascimento(string? dataNascimento, List<DetalheErro> detalhes)
    {
        if (!DateOnly.TryParseExact(dataNascimento.ToString(), 
            "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data))
        {
            detalhes.Add(new DetalheErro("DataNascimento", "formato_invalido"));
            return null;
        }

        if (data > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            detalhes.Add(new DetalheErro("DataNascimento", "invalida"));
            return null;
        }
        
        return data;
    }

    public void AlterarStatus(StatusBeneficiario novoStatus)
    {
        if (Status.Equals(novoStatus))
        {
            throw new ConflitoException($"O beneficiário já está com o status '{novoStatus}'.");
        }
        Status = novoStatus;
    }

    public void Excluir()
    {
        if (ExcluidoEm.HasValue)
        {
            throw new ConflitoException("O beneficiário já foi excluído.");
        }
        ExcluidoEm = DateTime.UtcNow;
    }

    public void AtualizarDadosCadastrais(string? nomeCompleto, string? dataNascimento, Guid planoId)
    {
        var detalhes = new List<DetalheErro>();

        ValidarNome(nomeCompleto, detalhes);

        var dataNasc = ValidarDataNascimento(dataNascimento, detalhes);

        if (detalhes.Count > 0)
        {
            throw new ValidacaoException("Dados do beneficiário inválidos", detalhes);
        }

        NomeCompleto = nomeCompleto!.Trim();
        DataNascimento = dataNasc!.Value;
        PlanoId = planoId;
    }
    private bool CpfValido(string cpf)
    {
        int[] numeros = cpf
            .Select(c => c - '0')
            .ToArray();

        int soma = 0;

        for (int i = 0; i < 9; i++)
            soma += numeros[i] * (10 - i);

        int resto = soma % 11;
        int primeiroDigito = resto < 2 ? 0 : 11 - resto;

        if (numeros[9] != primeiroDigito)
            return false;

        soma = 0;

        for (int i = 0; i < 10; i++)
            soma += numeros[i] * (11 - i);

        resto = soma % 11;
        int segundoDigito = resto < 2 ? 0 : 11 - resto;

        return numeros[10] == segundoDigito;
    }
}
