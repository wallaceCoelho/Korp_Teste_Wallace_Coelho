import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { Category } from '../models/category.models';

@Component({
  selector: 'app-category-table',
  standalone: true,
  imports: [CommonModule, DatePipe, LucideAngularModule],
  templateUrl: './category-table.component.html'
})
export class CategoryTableComponent {
  @Input() categories: Category[] = [];
  @Output() edit = new EventEmitter<Category>();
  @Output() delete = new EventEmitter<string>();
}
