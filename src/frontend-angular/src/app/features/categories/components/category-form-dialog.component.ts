import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Category, CreateCategoryDto, UpdateCategoryDto } from '../models/category.models';
import { DrawerComponent } from '../../../shared/ui/drawer/drawer.component';

@Component({
  selector: 'app-category-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DrawerComponent],
  templateUrl: './category-form-dialog.component.html'
})
export class CategoryFormDialogComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() categoryToEdit: Category | null = null;
  @Input() isSubmitting = false;

  @Output() saveCreate = new EventEmitter<CreateCategoryDto>();
  @Output() saveUpdate = new EventEmitter<{ id: string; dto: UpdateCategoryDto }>();
  @Output() close = new EventEmitter<void>();

  form: FormGroup;

  constructor(private fb: FormBuilder) {
    this.form = this.fb.group({
      name: ['', [Validators.required]]
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['categoryToEdit']) {
      if (this.categoryToEdit) {
        this.form.patchValue({
          name: this.categoryToEdit.name
        });
      } else {
        this.form.reset({
          name: ''
        });
      }
    }
  }

  onSubmit() {
    if (this.form.invalid) return;

    const val = this.form.value;

    if (this.categoryToEdit) {
      const updateDto: UpdateCategoryDto = {
        id: this.categoryToEdit.id,
        name: val.name
      };
      this.saveUpdate.emit({ id: this.categoryToEdit.id, dto: updateDto });
    } else {
      const createDto: CreateCategoryDto = {
        name: val.name
      };
      this.saveCreate.emit(createDto);
    }
  }

  onClose() {
    this.close.emit();
  }
}
