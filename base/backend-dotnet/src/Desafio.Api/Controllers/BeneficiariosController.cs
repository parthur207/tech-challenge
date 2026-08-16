using Desafio.Api.Api.Contratos;
using Desafio.Api.Aplicacao;
using Desafio.Api.Dominio;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Desafio.Api.Controllers;

[ApiController]
[Route("beneficiarios")]
[Produces("application/json")]
public class BeneficiariosController(BeneficiarioServico _servico) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<EnvelopePaginado<BeneficiarioResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar(
        [FromQuery(Name = "pagina")] int? pagina,
        [FromQuery(Name = "tamanho")] int? tamanho,
        [FromQuery(Name = "status")] StatusBeneficiario? status,
        [FromQuery(Name = "plano_id")] Guid? planoId,
        CancellationToken cancellationToken)
    {
        var resultado = await _servico.ListarAsync(pagina, tamanho, status, planoId, cancellationToken);

        var envelope = new EnvelopePaginado<BeneficiarioResponse>(
            resultado.Dados.Select(BeneficiarioResponse.De).ToList(),
            resultado.Pagina,
            resultado.Tamanho,
            resultado.Total);

        return Ok(envelope);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id, CancellationToken cancellationToken)
    {
        var beneficiario = await _servico.ObterAsync(id, cancellationToken);

        return Ok(BeneficiarioResponse.De(beneficiario));
    }

    [HttpPost]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar(
        [FromBody] BeneficiarioRequestCriacao requisicao,
        CancellationToken cancellationToken)
    {
        var dados = new BeneficiarioCriacaoDados(
            requisicao.NomeCompleto,
            requisicao.Cpf,
            requisicao.DataNascimento?.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
            requisicao.PlanoId!.Value);

        var beneficiario = await _servico.CriarAsync(dados, cancellationToken);

        return CreatedAtAction(nameof(Obter), new { id = beneficiario.Id }, BeneficiarioResponse.De(beneficiario));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<BeneficiarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Atualizar(
        Guid id,
        [FromBody] BeneficiarioRequestAtualizacao requisicao,
        CancellationToken cancellationToken)
    {
        var dados = new BeneficiarioAtualizacaoDados(
            requisicao.NomeCompleto,
            requisicao.DataNascimento?.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
            requisicao.PlanoId!.Value,
            requisicao.Status);

        var beneficiario = await _servico.AtualizarAsync(id, dados, cancellationToken);

        return Ok(BeneficiarioResponse.De(beneficiario));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErroResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        await _servico.ExcluirAsync(id, cancellationToken);

        return NoContent();
    }
}