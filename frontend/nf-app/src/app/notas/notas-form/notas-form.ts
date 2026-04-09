import { Component, OnInit, Inject, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { NotaFiscalService, NotaFiscal } from '../../services/nota-fiscal.service';
import { ProdutoService, Produto } from '../../services/produto.service';

@Component({
  selector: 'app-notas-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './notas-form.html',
  styleUrl: './notas-form.scss'
})
export class NotasFormComponent implements OnInit {
  form!: FormGroup;
  salvando = false;
  editando = false;
  produtos: Produto[] = [];

  constructor(
    private fb: FormBuilder,
    private notaService: NotaFiscalService,
    private produtoService: ProdutoService,
    private snackBar: MatSnackBar,
    private dialogRef: MatDialogRef<NotasFormComponent>,
    private cdr: ChangeDetectorRef,
    @Inject(MAT_DIALOG_DATA) public data: NotaFiscal | null
  ) {}

  ngOnInit(): void {
    this.editando = !!this.data;

    this.form = this.fb.group({
      itens: this.fb.array([])
    });

    // Carrega lista de produtos disponíveis para o select
    this.produtoService.listar().subscribe({
      next: lista => {
        this.produtos = [...lista];

        // Se algum item da nota referencia produto excluído, mantém um placeholder
        if (this.editando && this.data?.itens) {
          this.data.itens.forEach(item => {
            if (!this.produtos.some(p => p.id === item.produtoId)) {
              this.produtos.push({
                id: item.produtoId,
                codigo: `#${item.produtoId}`,
                descricao: `${item.produtoDescricao} (produto removido)`,
                saldo: 0
              });
            }
            this.adicionarItem(item.produtoId, item.quantidade, item.precoUnitario);
          });
        } else {
          this.adicionarItem(); // começa com uma linha em branco
        }

        this.cdr.detectChanges();
      },
      error: err => this.snackBar.open(err.message, 'Fechar', { duration: 4000 })
    });
  }

  // FormArray: lista dinâmica de itens — cada item é um FormGroup
  get itens(): FormArray {
    return this.form.get('itens') as FormArray;
  }

  adicionarItem(produtoId: number | null = null, quantidade: number = 1, precoUnitario: number = 0): void {
    this.itens.push(this.fb.group({
      produtoId:     [produtoId,     Validators.required],
      quantidade:    [quantidade,    [Validators.required, Validators.min(1)]],
      precoUnitario: [precoUnitario, [Validators.required, Validators.min(0)]]
    }));
  }

  removerItem(index: number): void {
    this.itens.removeAt(index);
  }

  // Retorna a descrição do produto selecionado para salvar junto ao item
  descricaoProduto(produtoId: number): string {
    return this.produtos.find(p => p.id === produtoId)?.descricao ?? '';
  }

  compareProdutos = (a: number | null, b: number | null): boolean => {
    if (a === null || b === null) return false;
    return Number(a) === Number(b);
  };

  salvar(): void {
    if (this.form.invalid) return;
    this.salvando = true;

    const nota: NotaFiscal = {
      ...this.data,
      numero: this.data?.numero ?? '',
      status: 'Rascunho',
      itens: this.form.value.itens.map((item: any) => ({
        ...item,
        produtoDescricao: this.descricaoProduto(item.produtoId)
      }))
    };

    const operacao = (this.editando
      ? this.notaService.atualizar(this.data!.id!, nota)
      : this.notaService.criar(nota)) as Observable<unknown>;

    operacao.subscribe({
      next: () => {
        this.snackBar.open(
          this.editando ? 'Nota atualizada.' : 'Nota criada.',
          'Fechar',
          { duration: 3000 }
        );
        this.dialogRef.close(true);
      },
      error: (err: Error) => {
        this.snackBar.open(err.message, 'Fechar', { duration: 4000 });
        this.salvando = false;
      }
    });
  }

  cancelar(): void {
    this.dialogRef.close(false);
  }
}
