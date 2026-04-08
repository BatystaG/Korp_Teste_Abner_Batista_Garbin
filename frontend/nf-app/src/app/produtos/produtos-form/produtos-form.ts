import { Component, OnInit, Inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { ProdutoService, Produto } from '../../services/produto.service';

@Component({
  selector: 'app-produtos-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatInputModule,
    MatFormFieldModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './produtos-form.html',
  styleUrl: './produtos-form.scss'
})
export class ProdutosFormComponent implements OnInit {
  form!: FormGroup;
  salvando = false;
  editando = false;

  constructor(
    private fb: FormBuilder,
    private produtoService: ProdutoService,
    private snackBar: MatSnackBar,
    private dialogRef: MatDialogRef<ProdutosFormComponent>,
    // MAT_DIALOG_DATA injeta os dados passados ao abrir o dialog
    // Se veio um produto, estamos editando; se veio null, estamos criando
    @Inject(MAT_DIALOG_DATA) public data: Produto | null
  ) {}

  ngOnInit(): void {
    this.editando = !!this.data;
    this.form = this.fb.group({
      codigo:   [this.data?.codigo   ?? '', Validators.required],
      descricao:[this.data?.descricao ?? '', Validators.required],
      saldo:    [this.data?.saldo    ?? 0,  [Validators.required, Validators.min(0)]]
    });
  }

  salvar(): void {
    if (this.form.invalid) return;
    this.salvando = true;

    const produto: Produto = { ...this.data, ...this.form.value };

    const operacao = this.editando
      ? this.produtoService.atualizar(this.data!.id!, produto)
      : this.produtoService.criar(produto);

    operacao.subscribe({
      next: () => {
        this.snackBar.open(
          this.editando ? 'Produto atualizado.' : 'Produto criado.',
          'Fechar',
          { duration: 3000 }
        );
        this.dialogRef.close(true);
      },
      error: err => {
        this.snackBar.open(err.message, 'Fechar', { duration: 4000 });
        this.salvando = false;
      }
    });
  }

  cancelar(): void {
    this.dialogRef.close(false);
  }
}
