import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

export type ConfirmDialogVariant = 'danger' | 'success' | 'warning' | 'primary';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './confirm-dialog.component.html'
})
export class ConfirmDialogComponent {
  @Input() isOpen = false;
  @Input() title = 'Confirmar Ação';
  @Input() message = 'Tem certeza que deseja prosseguir com esta ação?';
  @Input() detailMessage = '';
  @Input() confirmText = 'Confirmar';
  @Input() cancelText = 'Cancelar';
  @Input() variant: ConfirmDialogVariant = 'primary';
  @Input() icon = 'help-circle';
  @Input() isLoading = false;

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  onConfirm(): void {
    if (!this.isLoading) {
      this.confirm.emit();
    }
  }

  onCancel(): void {
    if (!this.isLoading) {
      this.cancel.emit();
    }
  }
}
