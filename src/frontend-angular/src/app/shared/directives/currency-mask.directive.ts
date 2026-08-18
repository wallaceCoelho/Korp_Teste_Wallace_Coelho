import { Directive, ElementRef, HostListener, forwardRef, inject } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Directive({
  selector: '[appCurrencyMask]',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CurrencyMaskDirective),
      multi: true
    }
  ]
})
export class CurrencyMaskDirective implements ControlValueAccessor {
  private el = inject(ElementRef<HTMLInputElement>);

  private onChange: (val: number | null) => void = () => {};
  private onTouched: () => void = () => {};

  private rawNumericValue: number | null = null;

  writeValue(value: number | null): void {
    this.rawNumericValue = value;
    if (value === null || value === undefined || isNaN(value) || value === 0) {
      this.el.nativeElement.value = value === 0 ? this.formatCurrency(0) : '';
    } else {
      this.el.nativeElement.value = this.formatCurrency(value);
    }
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  @HostListener('input', ['$event.target.value'])
  onInput(value: string): void {
    const digits = value.replace(/\D/g, '');
    if (!digits) {
      this.rawNumericValue = null;
      this.el.nativeElement.value = '';
      this.onChange(null);
      return;
    }

    const num = parseInt(digits, 10) / 100;
    this.rawNumericValue = num;
    this.el.nativeElement.value = this.formatCurrency(num);
    this.onChange(num);
  }

  @HostListener('blur')
  onBlur(): void {
    this.onTouched();
    if (this.rawNumericValue !== null) {
      this.el.nativeElement.value = this.formatCurrency(this.rawNumericValue);
    }
  }

  private formatCurrency(value: number): string {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(value);
  }
}
