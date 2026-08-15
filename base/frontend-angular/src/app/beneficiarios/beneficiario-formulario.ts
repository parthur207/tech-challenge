import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';

import { mensagemDeErro } from '../nucleo/api';
import { Plano } from '../planos/plano';
import { Beneficiario, StatusBeneficiario } from './beneficiario';
import { BeneficiarioServico } from './beneficiario-servico';

function cpfValidator(controle: AbstractControl): ValidationErrors | null {
  const valor = (controle.value ?? '').toString().trim();
  return /^[0-9]{11}$/.test(valor) ? null : { cpfInvalido: true };
}

function dataPassadaValidator(controle: AbstractControl): ValidationErrors | null {
  if (!controle.value) {
    return null;
  }

  const hoje = new Date();
  hoje.setHours(0, 0, 0, 0);

  const data = new Date(`${controle.value}T00:00:00`);

  return data < hoje ? null : { dataFutura: true };
}

@Component({
  selector: 'app-beneficiario-formulario',
  imports: [ReactiveFormsModule],
  templateUrl: './beneficiario-formulario.html',
  styleUrl: './beneficiario-formulario.css'
})
export class BeneficiarioFormulario {
  private readonly servico = inject(BeneficiarioServico);
  private readonly fb = inject(FormBuilder);

  private readonly destroyRef = inject(DestroyRef);

  @Input() beneficiario: Beneficiario | null = null;
  @Input() planos: Plano[] = [];

  @Output() readonly salvo = new EventEmitter<void>();
  @Output() readonly cancelado = new EventEmitter<void>();

  protected readonly salvando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly formulario = this.fb.nonNullable.group({
    nomeCompleto: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(120)]],
    cpf: ['', [Validators.required, cpfValidator]],
    dataNascimento: ['', [Validators.required, dataPassadaValidator]],
    planoId: ['', Validators.required],
    status: ['ATIVO' as StatusBeneficiario]
  });

  constructor() {
    if (this.beneficiario) {
      this.formulario.patchValue({
        nomeCompleto: this.beneficiario.nome_completo,
        cpf: this.beneficiario.cpf,
        dataNascimento: this.beneficiario.data_nascimento,
        planoId: this.beneficiario.plano_id,
        status: this.beneficiario.status
      });
      this.formulario.controls.cpf.disable();
    }
  }

  protected get editando(): boolean {
    return this.beneficiario !== null;
  }

  protected salvar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    this.erro.set(null);
    this.salvando.set(true);

    const valores = this.formulario.getRawValue();

    const requisicao = this.beneficiario
      ? this.servico.atualizar(this.beneficiario.id, {
          nome_completo: valores.nomeCompleto,
          data_nascimento: valores.dataNascimento,
          plano_id: valores.planoId,
          status: valores.status
        })
      : this.servico.criar({
          nome_completo: valores.nomeCompleto,
          cpf: valores.cpf,
          data_nascimento: valores.dataNascimento,
          plano_id: valores.planoId
        });

    requisicao.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.salvando.set(false);
        this.salvo.emit();
      },
      error: (resposta: HttpErrorResponse) => {
        this.salvando.set(false);
        this.erro.set(mensagemDeErro(resposta));
      }
    });
  }
}