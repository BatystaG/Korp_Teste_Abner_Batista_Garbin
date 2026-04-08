import { Routes } from '@angular/router';
import { ProdutosListaComponent } from './produtos/produtos-lista/produtos-lista';
import { NotasListaComponent } from './notas/notas-lista/notas-lista';

export const routes: Routes = [
  { path: '', redirectTo: 'produtos', pathMatch: 'full' },
  { path: 'produtos', component: ProdutosListaComponent },
  { path: 'notas', component: NotasListaComponent },
];
