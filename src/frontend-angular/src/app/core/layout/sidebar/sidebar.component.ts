import { Component, Input, Output, EventEmitter, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, LucideAngularModule],
  templateUrl: './sidebar.component.html'
})
export class SidebarComponent implements OnInit {
  @Input() isCollapsed = false;
  @Input() isMobileOpen = false;

  @Output() toggleCollapse = new EventEmitter<void>();
  @Output() closeMobile = new EventEmitter<void>();

  private router = inject(Router);

  isInventoryOpen = true;
  isCollapsedInventoryOpen = false;

  ngOnInit() {
    const url = this.router.url;
    if (url.includes('/products') || url.includes('/categories')) {
      this.isInventoryOpen = true;
    }
  }

  get isInventoryActive(): boolean {
    const url = this.router.url;
    return url.includes('/products') || url.includes('/categories');
  }

  toggleInventoryDropdown() {
    if (this.isCollapsed) {
      this.isCollapsedInventoryOpen = !this.isCollapsedInventoryOpen;
    } else {
      this.isInventoryOpen = !this.isInventoryOpen;
    }
  }

  onNavItemClick() {
    this.isCollapsedInventoryOpen = false;
    this.closeMobile.emit();
  }
}
