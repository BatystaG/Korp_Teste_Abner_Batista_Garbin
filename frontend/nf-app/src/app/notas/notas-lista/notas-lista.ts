import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { CommonModule } from '@angular/common';
import { NotaFiscalService, NotaFiscal } from '../../services/nota-fiscal.service';
import { NotasFormComponent } from '../notas-form/notas-form';

@Component({
  selector: 'app-notas-lista',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatChipsModule,
  ],
  templateUrl: './notas-lista.html',
  styleUrl: './notas-lista.scss'
})
export class NotasListaComponent implements OnInit, OnDestroy {
  notas: NotaFiscal[] = [];
  colunas = ['numero', 'dataEmissao', 'status', 'itens', 'acoes'];
  carregando = false;
  imprimindo: number | null = null; // id da nota sendo impressa

  private destroy$ = new Subject<void>();

  constructor(
    private notaService: NotaFiscalService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.carregar();
  }

  carregar(): void {
    this.carregando = true;
    this.notaService.listar()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: dados => {
          this.notas = dados;
          this.carregando = false;
        },
        error: err => {
          this.snackBar.open(err.message, 'Fechar', { duration: 4000 });
          this.carregando = false;
        }
      });
  }

  abrirFormulario(nota?: NotaFiscal): void {
    const ref = this.dialog.open(NotasFormComponent, {
      width: '680px',
      data: nota ?? null
    });
    ref.afterClosed().subscribe(salvo => {
      if (salvo) this.carregar();
    });
  }

  imprimir(nota: NotaFiscal): void {
    if (nota.status !== 'Rascunho') {
      this.snackBar.open('Apenas notas com status Rascunho podem ser impressas.', 'Fechar', { duration: 4000 });
      return;
    }
    this.imprimindo = nota.id!;
    this.notaService.imprimir(nota.id!)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.snackBar.open('Nota impressa com sucesso!', 'Fechar', { duration: 3000 });
          this.imprimindo = null;
          this.carregar();
        },
        error: err => {
          this.snackBar.open(err.message, 'Fechar', { duration: 4000 });
          this.imprimindo = null;
        }
      });
  }

  excluir(nota: NotaFiscal): void {
    if (!confirm(`Excluir nota "${nota.numero}"?`)) return;
    this.notaService.excluir(nota.id!)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.snackBar.open('Nota excluída.', 'Fechar', { duration: 3000 });
          this.carregar();
        },
        error: err => this.snackBar.open(err.message, 'Fechar', { duration: 4000 })
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
