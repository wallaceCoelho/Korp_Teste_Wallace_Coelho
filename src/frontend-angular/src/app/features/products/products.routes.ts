import { Routes } from '@angular/router';
import { ProductListPageComponent } from './pages/product-list-page.component';

export const PRODUCTS_ROUTES: Routes = [
  {
    path: '',
    component: ProductListPageComponent
  }
];
