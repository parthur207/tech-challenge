using System.Globalization;
using Desafio.Api.Dominio;
using Desafio.Api.Infraestrutura;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Desafio.Api.Aplicacao;

public class BeneficiarioServico(AppDbContext _db)
{
    private const string CodigoViolacaoDeUnicidade = "23505";
    private const int TamanhoPadraoDaPagina = 10;

    public async Task<ResultadoPaginado> ListarAsync(
        int? pagina,
        int? tamanho,
        StatusBeneficiario? status,
        Guid? planoId,
        CancellationToken cancellationToken)
    {
        var paginaEfetiva = pagina ?? 1;
        var tamanhoEfetivo = tamanho ?? TamanhoPadraoDaPagina;

        GarantirPaginacaoValida(paginaEfetiva, tamanhoEfetivo);

        var consulta = _db.Beneficiarios.AsNoTracking().AsQueryable();

        if (status is not null)
        {
            consulta = consulta.Where(b => b.Status == status);
        }

        if (planoId is not null)
        {
            consulta = consulta.Where(b => b.PlanoId == planoId);
        }

        var total = await consulta.CountAsync(cancellationToken);

        var dados = await consulta
            .OrderBy(b => b.DataCadastro)
            .ThenBy(b => b.Id)
            .Skip((paginaEfetiva - 1) * tamanhoEfetivo)
            .Take(tamanhoEfetivo)
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado(dados, paginaEfetiva, tamanhoEfetivo, total);
    }

    public async Task<Beneficiario> ObterAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Beneficiarios.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
               ?? throw new NaoEncontradoException("Beneficiário não encontrado");
    }

    public async Task<Beneficiario> CriarAsync(BeneficiarioCriacaoDados dados, CancellationToken cancellationToken)
    {
        var beneficiario = new Beneficiario(dados.NomeCompleto, dados.Cpf, dados.DataNascimento, dados.PlanoId);

        await GarantirPlanoValidoAsync(beneficiario.PlanoId, cancellationToken);
        await GarantirCpfUnicoAsync(beneficiario.Cpf, cancellationToken);

        _db.Beneficiarios.Add(beneficiario);
        await SalvarAsync(cancellationToken);

        return beneficiario;
    }

    public async Task<Beneficiario> AtualizarAsync(
        Guid id,
        BeneficiarioAtualizacaoDados dados,
        CancellationToken cancellationToken)
    {
        var beneficiario = await ObterAsync(id, cancellationToken);

        if (beneficiario.Status == StatusBeneficiario.INATIVO && DadosCadastraisMudaram(beneficiario, dados))
        {
            throw new ConflitoException(
                "Beneficiário inativo. Dados cadastrais congelados",
                [new DetalheErro("status", "beneficiario_inativo")]);
        }

        await GarantirPlanoValidoAsync(dados.PlanoId, cancellationToken);

        beneficiario.AtualizarDadosCadastrais(dados.NomeCompleto, dados.DataNascimento, dados.PlanoId);

        if( beneficiario.Status != dados.Status)
        {
            beneficiario.AlterarStatus(dados.Status);

        }

        await SalvarAsync(cancellationToken);

        return beneficiario;
    }

    public async Task ExcluirAsync(Guid id, CancellationToken cancellationToken)
    {
        var beneficiario = await ObterAsync(id, cancellationToken);

        if (beneficiario.ExcluidoEm.HasValue)
        {
            throw new NaoProcessavelException(
                "Beneficiário já se encontra excluído",
                [new DetalheErro("id", "ja_excluido")]);
        }

        beneficiario.Excluir();
        await SalvarAsync(cancellationToken);
    }

    private static void GarantirPaginacaoValida(int pagina, int tamanho)
    {
        var detalhes = new List<DetalheErro>();

        if (pagina < 1)
        {
            detalhes.Add(new DetalheErro("pagina", "invalido"));
        }

        if (tamanho is < 1 or > 100)
        {
            detalhes.Add(new DetalheErro("tamanho", "invalido"));
        }

        if (detalhes.Count > 0)
        {
            throw new ValidacaoException("Parâmetros de paginação inválidos", detalhes);
        }
    }

    private async Task GarantirPlanoValidoAsync(Guid planoId, CancellationToken cancellationToken)
    {
        var existe = await _db.Planos.AnyAsync(p => p.Id == planoId, cancellationToken);

        if (!existe)

        {
            throw new NaoProcessavelException(
                "Plano informado não existe",
                [new DetalheErro("plano_id", "nao_encontrado")]);
        }
    }

    private async Task GarantirCpfUnicoAsync(string cpf, CancellationToken cancellationToken)
    {
        var existe = await _db.Beneficiarios
            .IgnoreQueryFilters()
            .AnyAsync(b => b.Cpf == cpf, cancellationToken);

        if (existe)
        {
            throw new ConflitoException(
                "Já existe beneficiário cadastrado com esse CPF",
                [new DetalheErro("cpf", "duplicado")]);
        }
    }

    //Validação se os dados cadastrais foram alterados, para impedir que um beneficiário inativo tenha seus dados alterados
    private static bool DadosCadastraisMudaram(Beneficiario atual, BeneficiarioAtualizacaoDados dados) =>
        atual.NomeCompleto != dados.NomeCompleto?.Trim() ||
        atual.DataNascimento.ToString() != dados.DataNascimento?.Trim() ||
        atual.PlanoId != dados.PlanoId;

    private async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (EhViolacaoDeUnicidade(excecao))
        {
            throw new ConflitoException(
                "Já existe beneficiário cadastrado com esse CPF",
                [new DetalheErro("cpf", "duplicado")]);
        }
    }

    private static bool EhViolacaoDeUnicidade(DbUpdateException excecao) =>
        excecao.InnerException is PostgresException postgres &&
        postgres.SqlState == CodigoViolacaoDeUnicidade;
}

public sealed record BeneficiarioCriacaoDados(string? NomeCompleto, string? Cpf, string? DataNascimento, Guid PlanoId);

public sealed record BeneficiarioAtualizacaoDados(
    string? NomeCompleto,
    string? DataNascimento,
    Guid PlanoId,
    StatusBeneficiario Status);

public sealed record ResultadoPaginado(IReadOnlyList<Beneficiario> Dados, int Pagina, int Tamanho, int Total);