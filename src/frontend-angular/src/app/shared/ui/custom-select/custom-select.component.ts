import {
  Component,
  Input,
  Output,
  EventEmitter,
  forwardRef,
  ElementRef,
  HostListener,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';

export interface SelectOption {
  label: string;
  value: any;
  icon?: string;
  secondaryLabel?: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-custom-select',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CustomSelectComponent),
      multi: true,
    },
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './custom-select.component.html',
  styles: [`
    .custom-select-scroll::-webkit-scrollbar {
      width: 5px;
    }
    .custom-select-scroll::-webkit-scrollbar-track {
      background: transparent;
    }
    .custom-select-scroll::-webkit-scrollbar-thumb {
      background: rgba(148, 163, 184, 0.4);
      border-radius: 9999px;
    }
    :host-context(.dark) .custom-select-scroll::-webkit-scrollbar-thumb {
      background: rgba(51, 65, 85, 0.6);
    }
  `],
})
export class CustomSelectComponent implements ControlValueAccessor {
  @Input() options: SelectOption[] = [];
  @Input() placeholder: string = 'Selecione...';
  @Input() icon?: string;
  @Input() accentColor: 'blue' | 'emerald' = 'blue';
  @Input() searchable?: boolean;
  @Input() disabled: boolean = false;
  @Input() allowClear: boolean = true;
  @Input() clearValue: any = undefined;

  @Output() valueChange = new EventEmitter<any>();

  isOpen = false;
  searchQuery = '';
  value: any = null;

  private onChange: (value: any) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(
    private elementRef: ElementRef,
    private cdr: ChangeDetectorRef
  ) {}

  get shouldShowSearch(): boolean {
    if (this.searchable !== undefined) return this.searchable;
    return this.options && this.options.length > 7;
  }

  get selectedOption(): SelectOption | undefined {
    return this.options?.find(o => String(o.value) === String(this.value));
  }

  get canClear(): boolean {
    if (this.value === null || this.value === undefined || this.value === '') {
      return false;
    }
    // Se o valor selecionado for 'ALL', também não precisa do botão de limpar (já é o padrão)
    if (this.value === 'ALL') {
      return false;
    }
    return true;
  }

  filteredOptions(): SelectOption[] {
    if (!this.options) return [];
    if (!this.searchQuery.trim()) return this.options;
    const q = this.searchQuery.toLowerCase().trim();
    return this.options.filter(o => 
      o.label.toLowerCase().includes(q) || 
      (o.secondaryLabel && o.secondaryLabel.toLowerCase().includes(q))
    );
  }

  toggleDropdown() {
    if (this.disabled) return;
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      this.searchQuery = '';
    } else {
      this.onTouched();
    }
    this.cdr.markForCheck();
  }

  selectOption(opt: SelectOption, event?: Event) {
    if (event) event.stopPropagation();
    if (opt.disabled) return;
    this.value = opt.value;
    this.isOpen = false;
    this.onChange(this.value);
    this.onTouched();
    this.valueChange.emit(this.value);
    this.cdr.markForCheck();
  }

  clearSelection(event?: Event) {
    if (event) event.stopPropagation();
    
    // Determina o valor de reset apropriado
    let resetVal: any = this.clearValue;
    if (resetVal === undefined) {
      // Se existir uma opção 'ALL' na lista, reseta para 'ALL'
      const hasAllOption = this.options?.some(o => o.value === 'ALL');
      resetVal = hasAllOption ? 'ALL' : '';
    }

    this.value = resetVal;
    this.onChange(this.value);
    this.onTouched();
    this.valueChange.emit(this.value);
    this.cdr.markForCheck();
  }

  isSelected(opt: SelectOption): boolean {
    return String(this.value) === String(opt.value);
  }

  @HostListener('document:click', ['$event'])
  onClickOutside(event: MouseEvent) {
    if (this.isOpen && !this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen = false;
      this.onTouched();
      this.cdr.markForCheck();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape() {
    if (this.isOpen) {
      this.isOpen = false;
      this.cdr.markForCheck();
    }
  }

  // ControlValueAccessor Implementation
  writeValue(value: any): void {
    this.value = value;
    this.cdr.markForCheck();
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
    this.cdr.markForCheck();
  }
}
