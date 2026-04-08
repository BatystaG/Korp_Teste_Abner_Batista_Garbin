import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface Produto {
  id?: number;
  codigo: string;
  descricao: string;
  saldo: number;
}

// @Injectable({ providedIn: 'root' }) registra o service globalmente.
// Equivale a colocar no "provide" do Vue — qualquer componente pode injetá-lo.
@Injectable({ providedIn: 'root' })
export class ProdutoService {
  private url = `${environment.estoqueApiUrl}/api/produtos`;

  // HttpClient é injetado automaticamente pelo Angular (Dependency Injection)
  constructor(private http: HttpClient) {}

  listar(): Observable<Produto[]> {
    return this.http.get<Produto[]>(this.url).pipe(
      catchError(err => throwError(() => new Error('Serviço de estoque indisponível.')))
    );
  }

  buscarPorId(id: number): Observable<Produto> {
    return this.http.get<Produto>(`${this.url}/${id}`).pipe(
      catchError(err => throwError(() => new Error('Produto não encontrado.')))
    );
  }

  criar(produto: Produto): Observable<Produto> {
    return this.http.post<Produto>(this.url, produto).pipe(
      catchError(err => throwError(() => new Error('Erro ao criar produto.')))
    );
  }

  atualizar(id: number, produto: Produto): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, produto).pipe(
      catchError(err => throwError(() => new Error('Erro ao atualizar produto.')))
    );
  }

  excluir(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`).pipe(
      catchError(err => throwError(() => new Error('Erro ao excluir produto.')))
    );
  }
}
