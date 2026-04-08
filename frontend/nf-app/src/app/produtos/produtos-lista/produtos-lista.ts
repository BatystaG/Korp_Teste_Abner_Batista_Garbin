import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { ProdutoService, Produto } from '../../services/produto.service';
import { ProdutosFormComponent } from '../produtos-form/produtos-form';

@Component({
  selector: 'app-produtos-lista',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatDialogModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './produtos-lista.html',
  styleUrl: './produtos-lista.scss'
})
export class ProdutosListaComponent implements OnInit, OnDestroy {
  produtos: Produto[] = [];
  colunas = ['codigo', 'descricao', 'saldo', 'acoes'];
  carregando = false;

  // Subject usado para cancelar subscriptions ao destruir o componente (ngOnDestroy)
  // Evita memory leak — equivalente ao beforeDestroy do Vue
  private destroy$ = new Subject<void>();

  constructor(
    private produtoService: ProdutoService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {}

  // ngOnInit: busca os produtos assim que o componente é criado
  // Equivalente ao created() do Vue
  ngOnInit(): void {
    this.carregar();
  }

  carregar(): void {
    this.carregando = true;
    this.produtoService.listar()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: dados => {
          this.produtos = dados;
          this.carregando = false;
        },
        error: err => {
          this.snackBar.open(err.message, 'Fechar', { duration: 4000 });
          this.carregando = false;
        }
      });
  }

  abrirFormulario(produto?: Produto): void {
    const ref = this.dialog.open(ProdutosFormComponent, {
      width: '480px',
      data: produto ?? null
    });

    ref.afterClosed().subscribe(salvo => {
      if (salvo) this.carregar();
    });
  }

  excluir(produto: Produto): void {
    if (!confirm(`Excluir "${produto.descricao}"?`)) return;
    this.produtoService.excluir(produto.id!)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.snackBar.open('Produto excluído.', 'Fechar', { duration: 3000 });
          this.carregar();
        },
        error: err => this.snackBar.open(err.message, 'Fechar', { duration: 4000 })
      });
  }

  // ngOnDestroy: cancela todas as subscriptions ativas ao sair da tela
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
