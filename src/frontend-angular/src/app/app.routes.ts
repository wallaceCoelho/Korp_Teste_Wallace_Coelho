import { Routes } from '@angular/router';
import { AppLayoutComponent } from './core/layout/app-layout.component';

export const routes: Routes = [
  {
    path: '',
    component: AppLayoutComponent,
    children: [
      {
        path: '',
        redirectTo: 'products',
        pathMatch: 'full'
      },
      {
        path: 'products',
        loadChildren: () => import('./features/products/products.routes').then(m => m.PRODUCTS_ROUTES)
      },
      {
        path: 'categories',
        loadChildren: () => import('./features/categories/categories.routes').then(m => m.CATEGORIES_ROUTES)
      },
      {
        path: 'invoices',
        loadChildren: () => import('./features/invoices/invoices.routes').then(m => m.INVOICES_ROUTES)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'products'
  }
];
