import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { Invoice, InvoiceStatus } from '../models/invoice.models';
import { BadgeComponent } from '../../../shared/ui/badge/badge.component';

@Component({
  selector: 'app-invoice-table',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe, BadgeComponent, LucideAngularModule],
  templateUrl: './invoice-table.component.html'
})
export class InvoiceTableComponent {
  @Input() invoices: Invoice[] = [];
  @Input() processingId: string | null = null;
  @Input() processingActions: { [invoiceId: string]: string } = {};

  @Output() print = new EventEmitter<Invoice>();
  @Output() cancel = new EventEmitter<Invoice>();
  @Output() edit = new EventEmitter<Invoice>();
  @Output() viewReason = new EventEmitter<Invoice>();
  @Output() reprint = new EventEmitter<Invoice>();
  
  InvoiceStatus = InvoiceStatus;
}
