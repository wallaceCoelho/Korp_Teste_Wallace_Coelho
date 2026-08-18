import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { Product, StockOperationType } from '../models/product.models';

@Component({
  selector: 'app-stock-entry-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule],
  templateUrl: './stock-entry-dialog.component.html'
})
export class StockEntryDialogComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() product: Product | null = null;
  @Input() isSubmitting = false;

  @Output() confirm = new EventEmitter<{ productId: string; quantity: number; operation: StockOperationType }>();
  @Output() close = new EventEmitter<void>();

  StockOperationType = StockOperationType;
  form: FormGroup;

  constructor(private fb: FormBuilder) {
    this.form = this.fb.group({
      operationType: ['ADD', [Validators.required]], // 'ADD' or 'SUBTRACT'
      quantity: [10, [Validators.required, Validators.min(1)]],
      reason: ['']
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['product'] && this.product) {
      this.form.reset({
        operationType: 'ADD',
        quantity: 10,
        reason: ''
      });
    }
  }

  get currentStock(): number {
    return this.product?.stockQuantity ?? 0;
  }

  get isDeduct(): boolean {
    return this.form.get('operationType')?.value === 'SUBTRACT';
  }

  get rawQuantity(): number {
    return Math.abs(Number(this.form.get('quantity')?.value) || 0);
  }

  get quantityChange(): number {
    return this.isDeduct ? -this.rawQuantity : this.rawQuantity;
  }

  get projectedStock(): number {
    return this.currentStock + this.quantityChange;
  }

  onSubmit() {
    if (this.form.invalid || !this.product) return;
    
    if (this.projectedStock < 0) {
      alert('A quantidade resultante não pode ser negativa.');
      return;
    }

    const operation = this.isDeduct ? StockOperationType.Deduct : StockOperationType.Add;

    this.confirm.emit({
      productId: this.product.id,
      quantity: this.rawQuantity,
      operation: operation
    });
  }

  onClose() {
    this.close.emit();
  }
}
