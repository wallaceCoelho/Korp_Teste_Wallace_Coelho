import { Component, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { LucideAngularModule } from 'lucide-angular';
import { SidebarComponent } from './sidebar/sidebar.component';
import { ToastContainerComponent } from '../../shared/ui/toast/toast-container.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent, ToastContainerComponent, LucideAngularModule],
  templateUrl: './app-layout.component.html'
})
export class AppLayoutComponent {
  isSidebarCollapsed = signal<boolean>(false);
  isMobileSidebarOpen = signal<boolean>(false);
  isDarkMode = signal<boolean>(true);

  constructor() {
    effect(() => {
      const htmlEl = document.documentElement;
      if (this.isDarkMode()) {
        htmlEl.classList.add('dark');
      } else {
        htmlEl.classList.remove('dark');
      }
    });
  }

  toggleSidebar() {
    this.isSidebarCollapsed.update(val => !val);
  }

  openMobileSidebar() {
    this.isMobileSidebarOpen.set(true);
  }

  closeMobileSidebar() {
    this.isMobileSidebarOpen.set(false);
  }

  toggleTheme() {
    this.isDarkMode.update(val => !val);
  }
}
