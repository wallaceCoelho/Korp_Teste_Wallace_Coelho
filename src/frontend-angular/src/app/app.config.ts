import { ApplicationConfig, importProvidersFrom, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { 
  LucideAngularModule, 
  Package, 
  PackagePlus,
  Receipt, 
  Plus, 
  Search, 
  Printer, 
  Pencil, 
  Trash2, 
  Check, 
  X, 
  Sun, 
  Moon, 
  ChevronLeft, 
  ChevronRight, 
  ChevronDown,
  ChevronUp,
  AlertTriangle, 
  AlertCircle,
  CheckCircle2, 
  XCircle, 
  Info, 
  RefreshCw, 
  RotateCw,
  Layers,
  FolderTree,
  Tag,
  Filter,
  SlidersHorizontal,
  Clock,
  Calendar,
  Menu,
  Ban,
  Eye,
  ArrowDownCircle,
  ArrowUpCircle,
  Sparkles,
  Wand2,
  Bot
} from 'lucide-angular';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync(),
    importProvidersFrom(
      LucideAngularModule.pick({ 
        Package, 
        PackagePlus,
        Receipt, 
        Plus, 
        Search, 
        Printer, 
        Pencil, 
        Trash2, 
        Check, 
        X, 
        Sun, 
        Moon, 
        ChevronLeft, 
        ChevronRight, 
        ChevronDown,
        ChevronUp,
        AlertTriangle, 
        AlertCircle,
        CheckCircle2, 
        XCircle, 
        Info, 
        RefreshCw, 
        RotateCw,
        Layers,
        FolderTree,
        Tag,
        Filter,
        SlidersHorizontal,
        Clock,
        Calendar,
        Menu,
        Ban,
        Eye,
        ArrowDownCircle,
        ArrowUpCircle,
        Sparkles,
        Wand2,
        Bot
      })
    )
  ]
};
