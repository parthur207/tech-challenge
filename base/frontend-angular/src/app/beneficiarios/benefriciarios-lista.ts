import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { mensagemDeErro } from '../nucleo/api';
import { Plano } from '../planos/plano';
import { PlanoServico } from '../planos/plano-servico';
import { Beneficiario, StatusBeneficiario } from './beneficiario';
import { BeneficiarioServico } from './beneficiario-servico';

@Component({
  selector: 'app-beneficiarios-lista',
  templateUrl: './beneficiarios-lista.html',
  styleUrl: './beneficiarios-lista.css'
})
export class BeneficiariosLista {
  private readonly servico = inject(BeneficiarioServico);
  private readonly planoServico = inject(PlanoServico);
  
  private readonly destroyRef = inject(DestroyRef);

  protected readonly beneficiarios = signal<Beneficiario[]>([]);
  protected readonly planos = signal<Plano[]>([]);
  protected readonly carregando = signal(true);
  protected readonly erro = signal<string | null>(null);
  protected readonly erroExclusao = signal<string | null>(null);

  protected readonly pagina = signal(1);
  protected readonly tamanho = signal(10);
  protected readonly total = signal(0);
  protected readonly filtroStatus = signal<StatusBeneficiario | ''>('');
  protected readonly filtroPlanoId = signal('');

  protected readonly beneficiarioEmEdicao = signal<Beneficiario | null>(null);
  protected readonly mostrarFormulario = signal(false);

  protected readonly totalDePaginas = computed(() =>
    this.total() === 0 ? 1 : Math.ceil(this.total() / this.tamanho())
  );

  constructor() {
    this.planoServico
      .listar()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (planos) => this.planos.set(planos) });

    this.carregar();
  }

  protected nomeDoPlano(planoId: string): string {
   
    return this.planos().find((plano) => plano.id === planoId)?.nome ?? 'Plano removido';
  }

  protected aplicarFiltroStatus(valor: string): void {
    this.filtroStatus.set(valor as StatusBeneficiario | '');
    this.pagina.set(1);
    this.carregar();
  }

  protected aplicarFiltroPlano(valor: string): void {
    this.filtroPlanoId.set(valor);
    this.pagina.set(1);
    this.carregar();
  }

  protected irParaPagina(pagina: number): void {
    if (pagina < 1 || pagina > this.totalDePaginas()) {
      return;
    }

    this.pagina.set(pagina);
    this.carregar();
  }

  protected abrirCriacao(): void {
    this.beneficiarioEmEdicao.set(null);
    this.mostrarFormulario.set(true);
  }

  protected abrirEdicao(beneficiario: Beneficiario): void {
    this.beneficiarioEmEdicao.set(beneficiario);
    this.mostrarFormulario.set(true);
  }

  protected fecharFormulario(): void {
    this.mostrarFormulario.set(false);
  }

  protected aoSalvar(): void {
    this.mostrarFormulario.set(false);
    this.carregar();
  }

  protected excluir(beneficiario: Beneficiario): void {
    this.erroExclusao.set(null);

    this.servico
      .excluir(beneficiario.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.carregar(),
        error: (resposta: HttpErrorResponse) => this.erroExclusao.set(mensagemDeErro(resposta))
      });
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erro.set(null);

    this.servico
      .listar({
        pagina: this.pagina(),
        tamanho: this.tamanho(),
        status: this.filtroStatus() || undefined,
        plano_id: this.filtroPlanoId() || undefined
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (envelope) => {
          this.beneficiarios.set(envelope.dados);
          this.pagina.set(envelope.pagina);
          this.tamanho.set(envelope.tamanho);
          this.total.set(envelope.total);
          this.carregando.set(false);
        },
        error: (resposta: HttpErrorResponse) => {
          this.erro.set(mensagemDeErro(resposta));
          this.carregando.set(false);
        }
      });
  }
}