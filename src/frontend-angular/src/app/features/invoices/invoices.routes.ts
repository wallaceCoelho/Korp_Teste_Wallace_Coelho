import { Routes } from '@angular/router';
import { InvoiceListPageComponent } from './pages/invoice-list-page.component';

export const INVOICES_ROUTES: Routes = [
  {
    path: '',
    component: InvoiceListPageComponent
  }
];
