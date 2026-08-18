import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { CreateInvoiceDto, Invoice, InvoiceProductOption } from '../models/invoice.models';
import { DrawerComponent } from '../../../shared/ui/drawer/drawer.component';
import { BadgeComponent } from '../../../shared/ui/badge/badge.component';
import { CustomSelectComponent, SelectOption } from '../../../shared/ui/custom-select/custom-select.component';

export interface DraftInvoiceItem {
  tempId: string;
  productId: string;
  product: InvoiceProductOption;
  quantity: number;
  subtotal: number;
}

@Component({
  selector: 'app-new-invoice-dialog',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    CurrencyPipe, 
    DrawerComponent, 
    BadgeComponent, 
    LucideAngularModule,
    CustomSelectComponent
  ],
  templateUrl: './new-invoice-dialog.component.html'
})
export class NewInvoiceDialogComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() availableProducts: InvoiceProductOption[] = [];
  @Input() invoiceToEdit: Invoice | null = null;
  @Input() isSubmitting = false;

  @Output() create = new EventEmitter<CreateInvoiceDto>();
  @Output() update = new EventEmitter<{ id: string; dto: CreateInvoiceDto }>();
  @Output() close = new EventEmitter<void>();

  get productOptions(): SelectOption[] {
    return this.availableProducts.map((prod) => ({
      label: `${prod.code} - ${prod.name || prod.description}`,
      value: prod.id,
      secondaryLabel: `Saldo: ${prod.stockQuantity} un | R$ ${prod.unitPrice.toFixed(2)}`,
    }));
  }

  itemForm: FormGroup;
  draftItems: DraftInvoiceItem[] = [];
  selectedProduct: InvoiceProductOption | null = null;
  editingTempId: string | null = null;

  constructor(private fb: FormBuilder) {
    this.itemForm = this.fb.group({
      productId: ['', [Validators.required]],
      quantity: [1, [Validators.required, Validators.min(1)]]
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      this.populateFromInvoiceToEdit();
    } else if (changes['invoiceToEdit'] && this.isOpen) {
      this.populateFromInvoiceToEdit();
    }
  }

  get isEditMode(): boolean {
    return !!this.invoiceToEdit;
  }

  private populateFromInvoiceToEdit() {
    this.resetForm();

    if (this.invoiceToEdit && this.invoiceToEdit.items && this.invoiceToEdit.items.length > 0) {
      this.draftItems = this.invoiceToEdit.items.map(item => {
        const product = this.availableProducts.find(p => p.id === item.productId) || {
          id: item.productId,
          code: item.productCode,
          name: item.productDescription || item.productCode,
          description: item.productDescription || item.productCode,
          stockQuantity: 999,
          unitPrice: item.unitPrice
        };

        return {
          tempId: crypto.randomUUID(),
          productId: item.productId,
          product: product,
          quantity: item.quantity,
          subtotal: item.unitPrice * item.quantity
        };
      });
    } else {
      this.draftItems = [];
    }
  }

  private resetForm() {
    this.editingTempId = null;
    this.selectedProduct = null;
    this.itemForm.reset({
      productId: '',
      quantity: 1
    });
  }

  onProductChange() {
    const pId = this.itemForm.get('productId')?.value;
    this.selectedProduct = this.availableProducts.find(p => p.id === pId) || null;
  }

  get isStockExceeded(): boolean {
    if (!this.selectedProduct) return false;
    const requestedQty = this.itemForm.get('quantity')?.value || 0;
    
    const existingDraftQty = this.draftItems
      .filter(item => item.productId === this.selectedProduct!.id && item.tempId !== this.editingTempId)
      .reduce((sum, item) => sum + item.quantity, 0);

    return (requestedQty + existingDraftQty) > this.selectedProduct.stockQuantity;
  }

  get currentSubtotal(): number {
    if (!this.selectedProduct) return 0;
    const qty = this.itemForm.get('quantity')?.value || 0;
    if (qty <= 0) return 0;
    return this.selectedProduct.unitPrice * qty;
  }

  get grandTotal(): number {
    return this.draftItems.reduce((acc, item) => acc + item.subtotal, 0);
  }

  onAddOrUpdateItem() {
    if (this.itemForm.invalid || this.isStockExceeded || !this.selectedProduct) return;

    const pId = this.itemForm.get('productId')?.value;
    const qty = Number(this.itemForm.get('quantity')?.value);
    const subtotal = this.selectedProduct.unitPrice * qty;

    if (this.editingTempId) {
      const idx = this.draftItems.findIndex(i => i.tempId === this.editingTempId);
      if (idx !== -1) {
        this.draftItems[idx] = {
          tempId: this.editingTempId,
          productId: pId,
          product: this.selectedProduct,
          quantity: qty,
          subtotal: subtotal
        };
      }
      this.editingTempId = null;
    } else {
      const existingIdx = this.draftItems.findIndex(i => i.productId === pId);
      if (existingIdx !== -1) {
        this.draftItems[existingIdx].quantity = qty;
        this.draftItems[existingIdx].subtotal = this.selectedProduct.unitPrice * qty;
      } else {
        this.draftItems.push({
          tempId: crypto.randomUUID(),
          productId: pId,
          product: this.selectedProduct,
          quantity: qty,
          subtotal: subtotal
        });
      }
    }

    this.resetForm();
  }

  startEditingItem(item: DraftInvoiceItem) {
    this.editingTempId = item.tempId;
    this.selectedProduct = item.product;
    this.itemForm.patchValue({
      productId: item.productId,
      quantity: item.quantity
    });
  }

  cancelEditing() {
    this.resetForm();
  }

  removeDraftItem(tempId: string) {
    this.draftItems = this.draftItems.filter(i => i.tempId !== tempId);
    if (this.editingTempId === tempId) {
      this.cancelEditing();
    }
  }

  updateItemQuantity(tempId: string, rawVal: any) {
    const item = this.draftItems.find(i => i.tempId === tempId);
    if (!item) return;

    let qty = parseInt(rawVal, 10);
    if (isNaN(qty) || qty < 1) qty = 1;
    if (item.product.stockQuantity && qty > item.product.stockQuantity) {
      qty = item.product.stockQuantity;
    }

    item.quantity = qty;
    item.subtotal = qty * item.product.unitPrice;
  }

  incrementQuantity(tempId: string) {
    const item = this.draftItems.find(i => i.tempId === tempId);
    if (!item) return;

    const maxStock = item.product.stockQuantity || 9999;
    if (item.quantity < maxStock) {
      item.quantity++;
      item.subtotal = item.quantity * item.product.unitPrice;
    }
  }

  decrementQuantity(tempId: string) {
    const item = this.draftItems.find(i => i.tempId === tempId);
    if (!item) return;

    if (item.quantity > 1) {
      item.quantity--;
      item.subtotal = item.quantity * item.product.unitPrice;
    }
  }

  onFinalSubmit() {
    if (this.draftItems.length === 0) return;

    const dto: CreateInvoiceDto = {
      items: this.draftItems.map(i => ({
        productId: i.productId,
        productCode: i.product.code,
        productDescription: i.product.name || i.product.description || i.product.code,
        quantity: i.quantity,
        unitPrice: i.product.unitPrice
      }))
    };

    if (this.isEditMode && this.invoiceToEdit) {
      this.update.emit({ id: this.invoiceToEdit.id, dto });
    } else {
      this.create.emit(dto);
    }
  }

  onClose() {
    this.close.emit();
  }
}
