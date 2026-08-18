import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { Subject, Subscription, catchError, debounceTime, of, startWith, switchMap, tap, combineLatest } from 'rxjs';

import { ProductFeatureService } from '../services/product-feature.service';
import { Product, CreateProductDto, UpdateProductDto, ProductCategoryOption, StockOperationType } from '../models/product.models';
import { ProductTableComponent } from '../components/product-table.component';
import { ProductFormDialogComponent } from '../components/product-form-dialog.component';
import { StockEntryDialogComponent } from '../components/stock-entry-dialog.component';
import { PaginationComponent } from '../../../shared/ui/pagination/pagination.component';
import { ConfirmDialogComponent } from '../../../shared/ui/confirm-dialog/confirm-dialog.component';
import { CustomSelectComponent, SelectOption } from '../../../shared/ui/custom-select/custom-select.component';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-product-list-page',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    LucideAngularModule,
    ProductTableComponent, 
    ProductFormDialogComponent,
    StockEntryDialogComponent,
    PaginationComponent,
    ConfirmDialogComponent,
    CustomSelectComponent
  ],
  templateUrl: './product-list-page.component.html'
})
export class ProductListPageComponent implements OnInit, OnDestroy {
  private productService = inject(ProductFeatureService);
  private notificationService = inject(NotificationService);

  searchControl = new FormControl('');
  categoryFilterControl = new FormControl('ALL');

  products: Product[] = [];
  filteredProducts: Product[] = [];
  categories: ProductCategoryOption[] = [];
  isLoading = false;

  currentPage = 1;
  pageSize = 10;

  isModalOpen = false;
  selectedProduct: Product | null = null;
  isSubmitting = false;

  // Stock Entry Modal State
  isStockEntryOpen = false;
  productForStockEntry: Product | null = null;
  isStockSubmitting = false;

  // Confirmation Delete Modal State
  isConfirmDeleteOpen = false;
  productToDelete: Product | null = null;
  isDeleting = false;

  private refresh$ = new Subject<void>();
  private sub = new Subscription();

  get paginatedProducts(): Product[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredProducts.slice(start, start + this.pageSize);
  }

  get categoryOptions(): SelectOption[] {
    return [
      { label: 'Todas as Categorias', value: 'ALL' },
      ...this.categories.map(c => ({ label: c.name, value: c.id }))
    ];
  }

  ngOnInit(): void {
    this.loadCategories();

    const searchSub = combineLatest([
      this.searchControl.valueChanges.pipe(startWith(''), debounceTime(300)),
      this.categoryFilterControl.valueChanges.pipe(startWith('ALL'))
    ]).pipe(
      tap(() => {
        this.isLoading = true;
        this.currentPage = 1;
      }),
      switchMap(([searchTerm, categoryId]) =>
        this.productService.getProducts(searchTerm || '', categoryId || 'ALL').pipe(
          catchError(err => {
            this.notificationService.handleHttpError(err, 'Erro ao carregar produtos');
            return of({ items: [], totalCount: 0 });
          })
        )
      )
    ).subscribe((res: any) => {
      this.products = Array.isArray(res) ? res : (res?.items || []);
      this.applyCategoryFilter();
      this.isLoading = false;
    });

    const refreshSub = this.refresh$.pipe(
      tap(() => this.isLoading = true),
      switchMap(() =>
        this.productService.getProducts(
          this.searchControl.value || '', 
          this.categoryFilterControl.value || 'ALL'
        ).pipe(
          catchError(err => {
            this.notificationService.handleHttpError(err, 'Erro ao carregar produtos');
            return of({ items: [], totalCount: 0 });
          })
        )
      )
    ).subscribe((res: any) => {
      this.products = Array.isArray(res) ? res : (res?.items || []);
      this.applyCategoryFilter();
      this.isLoading = false;
    });

    this.sub.add(searchSub);
    this.sub.add(refreshSub);
  }

  loadCategories() {
    this.productService.getCategories().subscribe({
      next: (cats: any) => {
        this.categories = Array.isArray(cats) ? cats : (cats?.items || []);
      },
      error: () => (this.categories = []),
    });
  }

  applyCategoryFilter() {
    const catId = this.categoryFilterControl.value;
    if (!catId || catId === 'ALL') {
      this.filteredProducts = [...this.products];
    } else {
      this.filteredProducts = this.products.filter(
        p => p.categoryId === catId || p.category?.id === catId
      );
    }
  }

  onPageChange(page: number): void {
    this.currentPage = page;
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  refreshList() {
    this.refresh$.next();
  }

  openCreateModal() {
    this.loadCategories();
    this.selectedProduct = null;
    this.isModalOpen = true;
  }

  openEditModal(product: Product) {
    this.loadCategories();
    this.selectedProduct = product;
    this.isModalOpen = true;
  }

  closeModal() {
    this.isModalOpen = false;
    this.selectedProduct = null;
  }

  openStockEntryModal(product: Product) {
    this.productForStockEntry = product;
    this.isStockEntryOpen = true;
  }

  closeStockEntryModal() {
    this.isStockEntryOpen = false;
    this.productForStockEntry = null;
  }

  onConfirmStockEntry(event: { productId: string; quantity: number; operation: StockOperationType }) {
    this.isStockSubmitting = true;
    this.productService.updateStock(event.productId, event.quantity, event.operation).subscribe({
      next: () => {
        this.isStockSubmitting = false;
        this.closeStockEntryModal();
        const actionLabel = event.operation === StockOperationType.Add
          ? `Entrada de ${event.quantity} unidades realizada com sucesso!` 
          : `Saída/Ajuste de ${event.quantity} unidades realizado com sucesso!`;
        this.notificationService.success('Estoque Atualizado', actionLabel);
        this.refreshList();
      },
      error: (err) => {
        this.isStockSubmitting = false;
        this.notificationService.handleHttpError(err, 'Erro ao movimentar estoque');
      }
    });
  }

  onCreateProduct(dto: CreateProductDto) {
    this.isSubmitting = true;
    this.productService.createProduct(dto).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();
        this.notificationService.success('Produto Criado', `O produto "${dto.description || dto.name}" foi criado com sucesso.`);
        this.refreshList();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.notificationService.handleHttpError(err, 'Erro ao cadastrar produto');
      }
    });
  }

  onUpdateProduct(payload: { id: string; dto: UpdateProductDto; additionalStock?: number }) {
    this.isSubmitting = true;
    this.productService.updateProduct(payload.id, payload.dto).subscribe({
      next: () => {
        if (payload.additionalStock && payload.additionalStock > 0) {
          this.productService.updateStock(payload.id, payload.additionalStock, StockOperationType.Add).subscribe({
            next: () => {
              this.isSubmitting = false;
              this.closeModal();
              this.notificationService.success('Produto & Estoque Atualizados', `Produto atualizado e entrada de ${payload.additionalStock} un realizada.`);
              this.refreshList();
            },
            error: (err) => {
              this.isSubmitting = false;
              this.closeModal();
              this.notificationService.handleHttpError(err, 'Produto atualizado, mas ocorreu erro na entrada de estoque');
              this.refreshList();
            }
          });
        } else {
          this.isSubmitting = false;
          this.closeModal();
          this.notificationService.success('Produto Atualizado', `O produto foi atualizado com sucesso.`);
          this.refreshList();
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.notificationService.handleHttpError(err, 'Erro ao atualizar produto');
      }
    });
  }

  onDeleteProduct(id: string) {
    const prod = this.products.find(p => p.id === id);
    this.productToDelete = prod || { id, code: 'N/A', description: 'este produto' } as Product;
    this.isConfirmDeleteOpen = true;
  }

  onConfirmDeleteProduct() {
    if (!this.productToDelete) return;

    this.isDeleting = true;
    const prodDesc = this.productToDelete.description;
    this.productService.deleteProduct(this.productToDelete.id).subscribe({
      next: () => {
        this.isDeleting = false;
        this.isConfirmDeleteOpen = false;
        this.productToDelete = null;
        this.notificationService.success('Produto Excluído', `O produto "${prodDesc}" foi removido do catálogo.`);
        this.refreshList();
      },
      error: (err) => {
        this.isDeleting = false;
        this.notificationService.handleHttpError(err, 'Erro ao excluir produto');
      }
    });
  }

  onCancelDeleteProduct() {
    this.isConfirmDeleteOpen = false;
    this.productToDelete = null;
  }
}
