import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { Product } from '../models/product.models';
import { BadgeComponent } from '../../../shared/ui/badge/badge.component';

@Component({
  selector: 'app-product-table',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, BadgeComponent, LucideAngularModule],
  templateUrl: './product-table.component.html'
})
export class ProductTableComponent {
  @Input() products: Product[] = [];
  @Output() edit = new EventEmitter<Product>();
  @Output() delete = new EventEmitter<string>();
  @Output() stockEntry = new EventEmitter<Product>();
}
