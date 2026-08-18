import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './modal.component.html'
})
export class ModalComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() title = '';
  @Output() close = new EventEmitter<void>();

  isVisible = false;
  isAnimatingIn = false;
  private animTimeout: any;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      if (this.isOpen) {
        clearTimeout(this.animTimeout);
        this.isVisible = true;
        setTimeout(() => {
          this.isAnimatingIn = true;
        }, 20);
      } else {
        this.isAnimatingIn = false;
        clearTimeout(this.animTimeout);
        this.animTimeout = setTimeout(() => {
          this.isVisible = false;
        }, 300);
      }
    }
  }

  onBackdropClick() {
    this.close.emit();
  }
}
