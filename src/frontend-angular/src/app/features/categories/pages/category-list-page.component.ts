import { Component, OnInit, OnDestroy, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormControl, ReactiveFormsModule } from "@angular/forms";
import { LucideAngularModule } from "lucide-angular";
import {
  Subject,
  Subscription,
  catchError,
  debounceTime,
  of,
  startWith,
  switchMap,
  tap,
} from "rxjs";

import { CategoryFeatureService } from "../services/category-feature.service";
import {
  Category,
  CreateCategoryDto,
  UpdateCategoryDto,
} from "../models/category.models";
import { CategoryTableComponent } from "../components/category-table.component";
import { CategoryFormDialogComponent } from "../components/category-form-dialog.component";
import { PaginationComponent } from "../../../shared/ui/pagination/pagination.component";
import { ConfirmDialogComponent } from "../../../shared/ui/confirm-dialog/confirm-dialog.component";
import { NotificationService } from "../../../core/services/notification.service";

@Component({
  selector: "app-category-list-page",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    LucideAngularModule,
    CategoryTableComponent,
    CategoryFormDialogComponent,
    PaginationComponent,
    ConfirmDialogComponent,
  ],
  templateUrl: "./category-list-page.component.html",
})
export class CategoryListPageComponent implements OnInit, OnDestroy {
  private categoryService = inject(CategoryFeatureService);
  private notificationService = inject(NotificationService);

  searchControl = new FormControl("");
  categories: Category[] = [];
  isLoading = false;

  currentPage = 1;
  pageSize = 10;

  isModalOpen = false;
  selectedCategory: Category | null = null;
  isSubmitting = false;

  isConfirmDeleteOpen = false;
  categoryToDelete: Category | null = null;
  isDeleting = false;

  private refresh$ = new Subject<void>();
  private sub = new Subscription();

  get paginatedCategories(): Category[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.categories.slice(start, start + this.pageSize);
  }

  ngOnInit(): void {
    const searchSub = this.searchControl.valueChanges
      .pipe(
        startWith(""),
        debounceTime(300),
        tap(() => {
          this.isLoading = true;
          this.currentPage = 1;
        }),
        switchMap((searchTerm) =>
          this.categoryService.getCategories(searchTerm || "").pipe(
            catchError((err) => {
              this.notificationService.handleHttpError(
                err,
                "Erro ao carregar categorias",
              );
              return of({ items: [], totalCount: 0 });
            }),
          ),
        ),
      )
      .subscribe((res: any) => {
        this.categories = Array.isArray(res) ? res : res?.items || [];
        this.isLoading = false;
      });

    const refreshSub = this.refresh$
      .pipe(
        tap(() => (this.isLoading = true)),
        switchMap(() =>
          this.categoryService
            .getCategories(this.searchControl.value || "")
            .pipe(
              catchError((err) => {
                this.notificationService.handleHttpError(
                  err,
                  "Erro ao carregar categorias",
                );
                return of({ items: [], totalCount: 0 });
              }),
            ),
        ),
      )
      .subscribe((res: any) => {
        this.categories = Array.isArray(res) ? res : res?.items || [];
        this.isLoading = false;
      });

    this.sub.add(searchSub);
    this.sub.add(refreshSub);
  }

  onPageChange(page: number): void {
    this.currentPage = page;
  }

  refreshList() {
    this.refresh$.next();
  }

  openCreateModal() {
    this.selectedCategory = null;
    this.isModalOpen = true;
  }

  openEditModal(category: Category) {
    this.selectedCategory = category;
    this.isModalOpen = true;
  }

  closeModal() {
    this.isModalOpen = false;
    this.selectedCategory = null;
  }

  onCreateCategory(dto: CreateCategoryDto) {
    this.isSubmitting = true;
    this.categoryService.createCategory(dto).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();
        this.notificationService.success(
          "Categoria Cadastrada",
          `A categoria "${dto.name}" foi criada com sucesso.`,
        );
        this.refreshList();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.notificationService.handleHttpError(
          err,
          "Erro ao cadastrar categoria",
        );
      },
    });
  }

  onUpdateCategory(payload: { id: string; dto: UpdateCategoryDto }) {
    this.isSubmitting = true;
    this.categoryService.updateCategory(payload.id, payload.dto).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();
        this.notificationService.success(
          "Categoria Atualizada",
          `A categoria foi atualizada com sucesso.`,
        );
        this.refreshList();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.notificationService.handleHttpError(
          err,
          "Erro ao atualizar categoria",
        );
      },
    });
  }

  onDeleteCategory(id: string) {
    const cat = this.categories.find((c) => c.id === id);
    this.categoryToDelete = cat || ({ id, name: "esta categoria" } as Category);
    this.isConfirmDeleteOpen = true;
  }

  onConfirmDeleteCategory() {
    if (!this.categoryToDelete) return;

    this.isDeleting = true;
    const catName = this.categoryToDelete.name;
    this.categoryService.deleteCategory(this.categoryToDelete.id).subscribe({
      next: () => {
        this.isDeleting = false;
        this.isConfirmDeleteOpen = false;
        this.categoryToDelete = null;
        this.notificationService.success(
          "Categoria Excluída",
          `A categoria "${catName}" foi removida com sucesso.`,
        );
        this.refreshList();
      },
      error: (err) => {
        this.isDeleting = false;
        this.notificationService.handleHttpError(
          err,
          "Erro ao excluir categoria",
        );
      },
    });
  }

  onCancelDeleteCategory() {
    this.isConfirmDeleteOpen = false;
    this.categoryToDelete = null;
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }
}
