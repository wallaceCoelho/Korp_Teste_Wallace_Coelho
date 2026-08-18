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
  combineLatest,
} from "rxjs";

import { InvoiceFeatureService } from "../services/invoice-feature.service";
import { CategoryFeatureService } from "../../categories/services/category-feature.service";
import { ProductFeatureService } from "../../products/services/product-feature.service";
import {
  Invoice,
  CreateInvoiceDto,
  InvoiceProductOption,
  InvoiceStatus,
} from "../models/invoice.models";
import { Category } from "../../categories/models/category.models";
import { Product } from "../../products/models/product.models";
import { InvoiceTableComponent } from "../components/invoice-table.component";
import { NewInvoiceDialogComponent } from "../components/new-invoice-dialog.component";
import { InvoicePrintModalComponent } from "../components/invoice-print-modal.component";
import { PaginationComponent } from "../../../shared/ui/pagination/pagination.component";
import { ConfirmDialogComponent } from "../../../shared/ui/confirm-dialog/confirm-dialog.component";
import { CustomSelectComponent, SelectOption } from "../../../shared/ui/custom-select/custom-select.component";
import { NotificationService } from "../../../core/services/notification.service";

@Component({
  selector: "app-invoice-list-page",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    LucideAngularModule,
    InvoiceTableComponent,
    NewInvoiceDialogComponent,
    InvoicePrintModalComponent,
    PaginationComponent,
    ConfirmDialogComponent,
    CustomSelectComponent,
  ],
  templateUrl: "./invoice-list-page.component.html",
})
export class InvoiceListPageComponent implements OnInit, OnDestroy {
  private invoiceService = inject(InvoiceFeatureService);
  private categoryService = inject(CategoryFeatureService);
  private productService = inject(ProductFeatureService);
  private notificationService = inject(NotificationService);

  searchControl = new FormControl("");
  statusFilterControl = new FormControl("ALL");
  categoryFilterControl = new FormControl("ALL");

  invoices: Invoice[] = [];
  filteredInvoices: Invoice[] = [];
  availableProducts: InvoiceProductOption[] = [];
  categories: Category[] = [];
  allProducts: Product[] = [];

  isLoading = false;
  isModalOpen = false;
  isSubmitting = false;
  processingInvoiceId: string | null = null;
  processingActions: { [invoiceId: string]: string } = {};

  currentPage = 1;
  pageSize = 10;

  invoiceToEdit: Invoice | null = null;
  selectedReasonInvoice: Invoice | null = null;
  isReasonModalOpen = false;

  // Confirmation Modals State
  isConfirmCloseOpen = false;
  invoiceToClose: Invoice | null = null;

  isConfirmCancelOpen = false;
  invoiceToCancel: Invoice | null = null;

  // Print Template Modal State
  isPrintModalOpen = false;
  invoiceToPrint: Invoice | null = null;
  isDuplicatePrint = false;
  isAutoPrint = false;

  private refresh$ = new Subject<void>();
  private sub = new Subscription();

  get paginatedInvoices(): Invoice[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredInvoices.slice(start, start + this.pageSize);
  }

  get categoryOptions(): SelectOption[] {
    return [
      { label: "Todas as Categorias", value: "ALL" },
      ...this.categories.map((c) => ({ label: c.name, value: c.id })),
    ];
  }

  get statusOptions(): SelectOption[] {
    return [
      { label: "Todos os Status", value: "ALL" },
      { label: "Pendentes de Reserva", value: "5" },
      { label: "Abertas", value: "1" },
      { label: "Fechadas", value: "2" },
      { label: "Rejeitadas", value: "3" },
      { label: "Canceladas", value: "4" },
    ];
  }

  ngOnInit(): void {
    this.loadCategoriesAndProducts();

    const searchSub = combineLatest([
      this.searchControl.valueChanges.pipe(startWith(""), debounceTime(300)),
      this.statusFilterControl.valueChanges.pipe(startWith("ALL")),
      this.categoryFilterControl.valueChanges.pipe(startWith("ALL")),
    ])
      .pipe(
        tap(() => {
          this.isLoading = true;
          this.currentPage = 1;
        }),
        switchMap(([searchTerm]) => {
          return this.invoiceService.getInvoices(searchTerm ?? "").pipe(
            catchError((err) => {
              this.notificationService.handleHttpError(
                err,
                "Erro ao carregar faturas",
              );
              return of({ items: [], totalCount: 0 });
            }),
          );
        }),
      )
      .subscribe((res) => {
        this.invoices = res.items || [];
        this.cleanupCompletedProcessingActions();
        this.applyLocalFilters();
        this.isLoading = false;
      });

    const refreshSub = this.refresh$
      .pipe(
        tap(() => (this.isLoading = true)),
        switchMap(() =>
          this.invoiceService.getInvoices(this.searchControl.value ?? "").pipe(
            catchError((err) => {
              this.notificationService.handleHttpError(err);
              return of({ items: [], totalCount: 0 });
            }),
          ),
        ),
      )
      .subscribe((res) => {
        this.invoices = res.items || [];
        this.cleanupCompletedProcessingActions();
        this.applyLocalFilters();
        this.isLoading = false;
      });

    this.sub.add(searchSub);
    this.sub.add(refreshSub);

    this.loadProducts();
  }

  loadCategoriesAndProducts() {
    this.categoryService.getCategories().subscribe({
      next: (res: any) => (this.categories = Array.isArray(res) ? res : (res?.items || [])),
      error: () => (this.categories = []),
    });

    this.productService.getProducts("").subscribe({
      next: (res: any) => {
        this.allProducts = Array.isArray(res) ? res : (res?.items || []);
        this.applyLocalFilters();
      },
      error: () => (this.allProducts = []),
    });
  }

  loadProducts() {
    this.invoiceService.getAvailableProducts().subscribe({
      next: (products) => (this.availableProducts = products),
      error: (err) =>
        this.notificationService.handleHttpError(
          err,
          "Erro ao obter produtos para fatura",
        ),
    });
  }

  private cleanupCompletedProcessingActions() {
    for (const inv of this.invoices) {
      if (
        inv.status !== InvoiceStatus.Pending &&
        this.processingActions[inv.id]
      ) {
        delete this.processingActions[inv.id];
      }
    }
  }

  applyLocalFilters() {
    let result = [...this.invoices];

    // Filtro por Status
    const statusFilter = this.statusFilterControl.value;
    if (statusFilter && statusFilter !== "ALL") {
      const statusNum = Number(statusFilter);
      result = result.filter((i) => i.status === statusNum);
    }

    // Filtro por Categoria
    const categoryFilter = this.categoryFilterControl.value;
    if (categoryFilter && categoryFilter !== "ALL") {
      const productIdsInCategory = new Set(
        this.allProducts
          .filter(
            (p) =>
              p.categoryId === categoryFilter ||
              p.category?.id === categoryFilter,
          )
          .map((p) => p.id),
      );

      result = result.filter(
        (inv) =>
          inv.items &&
          inv.items.some((item) => productIdsInCategory.has(item.productId)),
      );
    }

    this.filteredInvoices = result;
  }

  onPageChange(page: number): void {
    this.currentPage = page;
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  openCreateModal() {
    this.invoiceToEdit = null;
    this.loadProducts();
    this.isModalOpen = true;
  }

  onEditInvoice(invoice: Invoice) {
    this.loadProducts();

    // Se os itens não estiverem no objeto local, busca os dados completos pelo ID
    if (!invoice.items || invoice.items.length === 0) {
      this.invoiceService.getInvoiceById(invoice.id).subscribe({
        next: (fullInvoice) => {
          this.invoiceToEdit = fullInvoice;
          this.isModalOpen = true;
        },
        error: () => {
          this.invoiceToEdit = invoice;
          this.isModalOpen = true;
        },
      });
    } else {
      this.invoiceToEdit = invoice;
      this.isModalOpen = true;
    }
  }

  closeModal() {
    this.isModalOpen = false;
    this.invoiceToEdit = null;
  }

  onCreateInvoice(dto: CreateInvoiceDto) {
    this.isSubmitting = true;
    this.invoiceService.createInvoice(dto).subscribe({
      next: (invoive) => {
        this.isSubmitting = false;
        this.closeModal();

        this.processingInvoiceId = invoive.id;
        this.processingActions[invoive.id] = "Abrindo NF...";

        this.listenToInvoiceSse(invoive.id, "create");
        this.notificationService.info(
          "Abrindo NF",
          "Solicitação enviada. Aguardando validação de estoque em tempo real...",
        );
      },
      error: (err) => {
        this.isSubmitting = false;
        this.notificationService.handleHttpError(err, "Erro ao criar fatura");
      },
    });
  }

  onUpdateInvoice(payload: { id: string; dto: CreateInvoiceDto }) {
    this.isSubmitting = true;
    this.invoiceService.updateInvoice(payload.id, payload.dto).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.closeModal();

        this.processingInvoiceId = payload.id;
        this.processingActions[payload.id] = "Reenviando...";

        const index = this.invoices.findIndex((i) => i.id === payload.id);
        if (index !== -1) {
          this.invoices[index] = {
            ...this.invoices[index],
            status: InvoiceStatus.Pending,
            statusDescription: "Pending",
          };
          this.applyLocalFilters();
        }

        this.listenToInvoiceSse(payload.id, "update");
        this.notificationService.info(
          "Reenviando NF",
          "Reenvio solicitado. Validando novo estoque em tempo real...",
        );
      },
      error: (err) => {
        this.isSubmitting = false;
        this.notificationService.handleHttpError(
          err,
          "Erro ao atualizar e reenviar fatura",
        );
      },
    });
  }

  private listenToInvoiceSse(
    invoiceId: string,
    context: "create" | "update" | "cancel" | "close",
  ) {
    const sseSub = this.invoiceService
      .listenInvoiceEvents(invoiceId)
      .subscribe({
        next: (updatedInvoice) => {
          const index = this.invoices.findIndex(
            (i) => i.id === updatedInvoice.id,
          );
          if (index !== -1) {
            const items =
              updatedInvoice.items && updatedInvoice.items.length > 0
                ? updatedInvoice.items
                : this.invoices[index].items;

            this.invoices[index] = { ...updatedInvoice, items };
          } else {
            this.invoices.unshift(updatedInvoice);
          }
          this.applyLocalFilters();

          if (updatedInvoice.status !== InvoiceStatus.Pending) {
            delete this.processingActions[updatedInvoice.id];
            if (this.processingInvoiceId === updatedInvoice.id) {
              this.processingInvoiceId = null;
            }
            this.loadProducts();

            const numStr = (updatedInvoice.number || 0)
              .toString()
              .padStart(5, "0");

            if (updatedInvoice.status === InvoiceStatus.Open) {
              this.notificationService.success(
                "NF Aberta com Sucesso!",
                `Fatura #${numStr} aberta e estoque reservado.`,
              );
            } else if (updatedInvoice.status === InvoiceStatus.Closed) {
              this.notificationService.success(
                "NF Fechada com Sucesso!",
                `Fatura #${numStr} fechada e estoque baixado com sucesso.`,
              );
              this.openPrintModal(
                this.invoices[index] || updatedInvoice,
                false,
                false,
              );
            } else if (updatedInvoice.status === InvoiceStatus.Canceled) {
              this.notificationService.success(
                "NF Cancelada",
                `Fatura #${numStr} cancelada com sucesso. Estoque liberado.`,
              );
            } else if (updatedInvoice.status === InvoiceStatus.Rejected) {
              const reason =
                updatedInvoice.reasonRejected ||
                "Falha ao processar validação no estoque.";
              if (context === "close") {
                this.notificationService.error(
                  "Falha ao Fechar NF",
                  `Não foi possível fechar a fatura #${numStr}: ${reason}`,
                );
              } else if (context === "update") {
                this.notificationService.error(
                  "Falha no Reenvio",
                  `Reenvio da fatura #${numStr} rejeitado: ${reason}`,
                );
              } else {
                this.notificationService.error(
                  "Reserva Rejeitada",
                  `Fatura #${numStr} rejeitada: ${reason}`,
                );
              }
            }
          }
        },
        error: (err) => {
          console.warn("SSE finalizado ou erro de stream:", err);
          delete this.processingActions[invoiceId];
          if (this.processingInvoiceId === invoiceId) {
            this.processingInvoiceId = null;
          }
          this.refresh$.next();
        },
      });

    this.sub.add(sseSub);
  }

  onPrintInvoice(invoice: Invoice) {
    this.invoiceToClose = invoice;
    this.isConfirmCloseOpen = true;
  }

  onConfirmCloseInvoice() {
    if (!this.invoiceToClose) return;

    const invoice = this.invoiceToClose;
    this.isConfirmCloseOpen = false;
    this.invoiceToClose = null;

    this.processingInvoiceId = invoice.id;
    this.processingActions[invoice.id] = "Fechando NF...";

    const index = this.invoices.findIndex((i) => i.id === invoice.id);
    if (index !== -1) {
      this.invoices[index] = {
        ...this.invoices[index],
        status: InvoiceStatus.Pending,
        statusDescription: "Pending",
      };
      this.applyLocalFilters();
    }

    const numStr = (invoice.number || 0).toString().padStart(5, "0");
    this.notificationService.info(
      "Fechando NF",
      `Processando fechamento da fatura #${numStr} e baixa definitiva no estoque...`,
    );

    this.invoiceService.printInvoice(invoice.id).subscribe({
      next: () => {
        this.listenToInvoiceSse(invoice.id, "close");
      },
      error: (err) => {
        delete this.processingActions[invoice.id];
        this.processingInvoiceId = null;
        this.notificationService.handleHttpError(
          err,
          "Erro ao solicitar fechamento da fatura",
        );
        this.refresh$.next();
      },
    });
  }

  onCancelCloseInvoice() {
    this.isConfirmCloseOpen = false;
    this.invoiceToClose = null;
  }

  onReprintInvoice(invoice: Invoice) {
    this.openPrintModal(
      invoice,
      invoice.status === InvoiceStatus.Closed,
      false,
    );
  }

  openPrintModal(
    invoice: Invoice,
    isDuplicate: boolean,
    autoPrint: boolean = false,
  ) {
    if (!invoice.items || invoice.items.length === 0) {
      this.invoiceService.getInvoiceById(invoice.id).subscribe({
        next: (fullInvoice) => {
          this.invoiceToPrint = fullInvoice;
          this.isDuplicatePrint = isDuplicate;
          this.isAutoPrint = autoPrint;
          this.isPrintModalOpen = true;
        },
        error: () => {
          this.invoiceToPrint = invoice;
          this.isDuplicatePrint = isDuplicate;
          this.isAutoPrint = autoPrint;
          this.isPrintModalOpen = true;
        },
      });
    } else {
      this.invoiceToPrint = invoice;
      this.isDuplicatePrint = isDuplicate;
      this.isAutoPrint = autoPrint;
      this.isPrintModalOpen = true;
    }
  }

  closePrintModal() {
    this.isPrintModalOpen = false;
    this.invoiceToPrint = null;
    this.isAutoPrint = false;
  }

  onCancelInvoice(invoice: Invoice) {
    this.invoiceToCancel = invoice;
    this.isConfirmCancelOpen = true;
  }

  onConfirmCancelInvoice() {
    if (!this.invoiceToCancel) return;

    const invoice = this.invoiceToCancel;
    this.isConfirmCancelOpen = false;
    this.invoiceToCancel = null;

    const numStr = (invoice.number || 0).toString().padStart(5, "0");
    this.processingInvoiceId = invoice.id;
    this.processingActions[invoice.id] = "Cancelando...";

    const index = this.invoices.findIndex((i) => i.id === invoice.id);
    if (index !== -1) {
      this.invoices[index] = {
        ...this.invoices[index],
        status: InvoiceStatus.Pending,
        statusDescription: "Pending",
      };
      this.applyLocalFilters();
    }

    this.notificationService.info(
      "Cancelando NF",
      `Solicitação de cancelamento da fatura #${numStr} enviada. Liberando estoque...`,
    );

    this.invoiceService.cancelInvoice(invoice.id).subscribe({
      next: () => {
        this.listenToInvoiceSse(invoice.id, "cancel");
      },
      error: (err) => {
        delete this.processingActions[invoice.id];
        this.processingInvoiceId = null;
        this.notificationService.handleHttpError(
          err,
          "Erro ao solicitar cancelamento da fatura",
        );
        this.refresh$.next();
      },
    });
  }

  onCancelCancelInvoice() {
    this.isConfirmCancelOpen = false;
    this.invoiceToCancel = null;
  }

  onViewReason(invoice: Invoice) {
    if (!invoice.items || invoice.items.length === 0) {
      this.invoiceService.getInvoiceById(invoice.id).subscribe({
        next: (fullInvoice) => {
          this.selectedReasonInvoice = fullInvoice;
          this.isReasonModalOpen = true;
        },
        error: () => {
          this.selectedReasonInvoice = invoice;
          this.isReasonModalOpen = true;
        },
      });
    } else {
      this.selectedReasonInvoice = invoice;
      this.isReasonModalOpen = true;
    }
  }

  closeReasonModal() {
    this.isReasonModalOpen = false;
    this.selectedReasonInvoice = null;
  }

  onEditFromReasonModal() {
    if (!this.selectedReasonInvoice) return;
    const inv = this.selectedReasonInvoice;
    this.closeReasonModal();
    this.onEditInvoice(inv);
  }
}
