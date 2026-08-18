import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './toast-container.component.html'
})
export class ToastContainerComponent {
  notificationService = inject(NotificationService);
}
