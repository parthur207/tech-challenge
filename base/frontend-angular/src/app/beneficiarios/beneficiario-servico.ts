import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE } from '../nucleo/api';
import {
  Beneficiario,
  BeneficiarioAtualizacao,
  BeneficiarioCriacao,
  EnvelopePaginado,
  FiltrosBeneficiarios
} from './beneficiario';

/**
 Requisição passa por um serviço. Componente não chama HttpClient direto.
 */
@Injectable({ providedIn: 'root' })
export class BeneficiarioServico {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE);

  listar(filtros: FiltrosBeneficiarios): Observable<EnvelopePaginado<Beneficiario>> {
    let parametros = new HttpParams();

    if (filtros.pagina) {
      parametros = parametros.set('pagina', filtros.pagina);
    }
    if (filtros.tamanho) {
      parametros = parametros.set('tamanho', filtros.tamanho);
    }
    if (filtros.status) {
      parametros = parametros.set('status', filtros.status);
    }
    if (filtros.plano_id) {
      parametros = parametros.set('plano_id', filtros.plano_id);
    }

    return this.http.get<EnvelopePaginado<Beneficiario>>(`${this.base}/beneficiarios`, {
      params: parametros
    });
  }

  criar(dados: BeneficiarioCriacao): Observable<Beneficiario> {
    return this.http.post<Beneficiario>(`${this.base}/beneficiarios`, dados);
  }

  atualizar(id: string, dados: BeneficiarioAtualizacao): Observable<Beneficiario> {
    return this.http.put<Beneficiario>(`${this.base}/beneficiarios/${id}`, dados);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/beneficiarios/${id}`);
  }
}
