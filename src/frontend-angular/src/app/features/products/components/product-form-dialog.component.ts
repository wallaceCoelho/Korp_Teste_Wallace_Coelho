import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Product, CreateProductDto, UpdateProductDto, ProductCategoryOption } from '../models/product.models';
import { DrawerComponent } from '../../../shared/ui/drawer/drawer.component';
import { CurrencyMaskDirective } from '../../../shared/directives/currency-mask.directive';
import { CustomSelectComponent, SelectOption } from '../../../shared/ui/custom-select/custom-select.component';

@Component({
  selector: 'app-product-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DrawerComponent, CurrencyMaskDirective, CustomSelectComponent],
  templateUrl: './product-form-dialog.component.html'
})
export class ProductFormDialogComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() productToEdit: Product | null = null;
  @Input() categories: ProductCategoryOption[] = [];
  @Input() isSubmitting = false;

  @Output() saveCreate = new EventEmitter<CreateProductDto>();
  @Output() saveUpdate = new EventEmitter<{ id: string; dto: UpdateProductDto }>();
  @Output() close = new EventEmitter<void>();

  get categoryOptions(): SelectOption[] {
    return [
      { label: 'Sem categoria (Nenhuma)', value: '' },
      ...this.categories.map(c => ({ label: c.name, value: c.id }))
    ];
  }

  form: FormGroup;

  constructor(private fb: FormBuilder) {
    this.form = this.fb.group({
      name: ['', [Validators.required]],
      code: ['', [Validators.required]],
      description: [''],
      categoryId: [''],
      unitPrice: [0, [Validators.required, Validators.min(0.01)]],
      initialStock: [10, [Validators.min(0)]],
      additionalStock: [0, [Validators.min(0)]],
      minStock: [null, [Validators.min(0)]]
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['productToEdit']) {
      if (this.productToEdit) {
        let name = this.productToEdit.name || '';
        let desc = this.productToEdit.description || '';
        if (!name && desc.includes(' - ')) {
          const parts = desc.split(' - ');
          name = parts[0];
          desc = parts.slice(1).join(' - ');
        } else if (!name) {
          name = desc;
          desc = '';
        }

        this.form.patchValue({
          name: name,
          code: this.productToEdit.code,
          description: desc,
          categoryId: this.productToEdit.categoryId || '',
          unitPrice: this.productToEdit.unitPrice,
          additionalStock: 0,
          minStock: this.productToEdit.minStockQuantity ?? null
        });
      } else {
        this.form.reset({
          name: '',
          code: '',
          description: '',
          categoryId: '',
          unitPrice: 0,
          initialStock: 10,
          additionalStock: 0,
          minStock: null
        });
      }
    }
  }

  onSubmit() {
    if (this.form.invalid) return;

    const val = this.form.value;

    if (this.productToEdit) {
      const updateDto: UpdateProductDto = {
        id: this.productToEdit.id,
        name: val.name,
        code: val.code,
        description: val.description || undefined,
        categoryId: val.categoryId || undefined,
        unitPrice: val.unitPrice,
        minStock: val.minStock != null ? val.minStock : undefined
      };
      this.saveUpdate.emit({ 
        id: this.productToEdit.id, 
        dto: updateDto,
        additionalStock: Number(val.additionalStock) || 0
      } as any);
    } else {
      const createDto: CreateProductDto = {
        name: val.name,
        code: val.code,
        description: val.description || undefined,
        categoryId: val.categoryId || undefined,
        unitPrice: val.unitPrice,
        initialStock: val.initialStock ?? 0,
        minStock: val.minStock != null ? val.minStock : undefined
      };
      this.saveCreate.emit(createDto);
    }
  }

  onClose() {
    this.close.emit();
  }
}
