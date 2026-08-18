import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type BadgeVariant = 'default' | 'success' | 'warning' | 'danger' | 'info' | 'outline';

@Component({
  selector: 'app-badge',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './badge.component.html'
})
export class BadgeComponent {
  @Input() variant: BadgeVariant = 'default';
}
