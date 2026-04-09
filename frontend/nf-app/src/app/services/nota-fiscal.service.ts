import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface ItemNota {
  id?: number;
  produtoId: number;
  produtoDescricao: string;
  quantidade: number;
  precoUnitario: number;
}

export interface NotaFiscal {
  id?: number;
  numero: string;
  dataEmissao?: string;
  status: string; // 'Rascunho' | 'Impressa'
  itens: ItemNota[];
}

@Injectable({ providedIn: 'root' })
export class NotaFiscalService {
  private url = `${environment.faturamentoApiUrl}/api/notas`;

  constructor(private http: HttpClient) {}

  private extrairMensagemErro(err: any, fallback: string): string {
    if (err?.error) {
      if (typeof err.error === 'string') {
        try {
          const parsed = JSON.parse(err.error);
          return this.extrairMensagemErro({ error: parsed }, fallback);
        } catch {
          return err.error;
        }
      }

      if (typeof err.error === 'object') {
        return err.error.erro ?? err.error.detail ?? err.error.title ?? err.error.message ?? fallback;
      }
    }

    return err?.message ?? fallback;
  }

  listar(): Observable<NotaFiscal[]> {
    return this.http.get<NotaFiscal[]>(this.url).pipe(
      catchError(err => throwError(() => new Error(this.extrairMensagemErro(err, 'Serviço de faturamento indisponível.'))))
    );
  }

  buscarPorId(id: number): Observable<NotaFiscal> {
    return this.http.get<NotaFiscal>(`${this.url}/${id}`).pipe(
      catchError(err => throwError(() => new Error(this.extrairMensagemErro(err, 'Nota fiscal não encontrada.'))))
    );
  }

  criar(nota: NotaFiscal): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(this.url, nota).pipe(
      catchError(err => throwError(() => new Error(this.extrairMensagemErro(err, 'Erro ao criar nota fiscal.'))))
    );
  }

  atualizar(id: number, nota: NotaFiscal): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, nota).pipe(
      catchError(err => throwError(() => new Error(this.extrairMensagemErro(err, 'Erro ao atualizar nota fiscal.'))))
    );
  }

  excluir(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`).pipe(
      catchError(err => throwError(() => new Error(this.extrairMensagemErro(err, 'Erro ao excluir nota fiscal.'))))
    );
  }

  // Chama o endpoint que debita o estoque e marca a nota como Impressa
  imprimir(id: number): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(`${this.url}/${id}/imprimir`, {}).pipe(
      catchError(err => throwError(() => new Error(this.extrairMensagemErro(err, 'Erro ao imprimir nota fiscal.'))))
    );
  }
}
