import {
  Component,
  EventEmitter,
  Input,
  Output,
  OnChanges,
  SimpleChanges,
  inject,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from "@angular/forms";
import { LucideAngularModule } from "lucide-angular";
import {
  Product,
  CreateProductDto,
  UpdateProductDto,
  ProductCategoryOption,
} from "../models/product.models";
import { DrawerComponent } from "../../../shared/ui/drawer/drawer.component";
import { CurrencyMaskDirective } from "../../../shared/directives/currency-mask.directive";
import {
  CustomSelectComponent,
  SelectOption,
} from "../../../shared/ui/custom-select/custom-select.component";
import {
  AiFeatureService,
  AiToneType,
} from "../../../core/services/ai-feature.service";

@Component({
  selector: "app-product-form-dialog",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    LucideAngularModule,
    DrawerComponent,
    CurrencyMaskDirective,
    CustomSelectComponent,
  ],
  templateUrl: "./product-form-dialog.component.html",
})
export class ProductFormDialogComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() productToEdit: Product | null = null;
  @Input() categories: ProductCategoryOption[] = [];
  @Input() isSubmitting = false;

  @Output() saveCreate = new EventEmitter<CreateProductDto>();
  @Output() saveUpdate = new EventEmitter<{
    id: string;
    dto: UpdateProductDto;
  }>();
  @Output() close = new EventEmitter<void>();

  private readonly fb = inject(FormBuilder);
  private readonly aiService = inject(AiFeatureService);

  // Estados da Inteligência Artificial
  isGeneratingAi = false;
  selectedTone: AiToneType = AiToneType.Minimalist;
  aiSuccessMessage: string | null = null;
  aiErrorMessage: string | null = null;

  readonly aiToneOptions: SelectOption[] = [
    { label: "Comercial & Vendedor", value: AiToneType.Commercial },
    { label: "Técnico & Detalhado", value: AiToneType.Technical },
    { label: "Persuasivo & Benefícios", value: AiToneType.Persuasive },
    { label: "Minimalista & Direto", value: AiToneType.Minimalist },
    { label: "Descontraído & Moderno", value: AiToneType.Casual },
  ];

  get categoryOptions(): SelectOption[] {
    return [
      { label: "Sem categoria (Nenhuma)", value: "" },
      ...this.categories.map((c) => ({ label: c.name, value: c.id })),
    ];
  }

  form: FormGroup = this.fb.group({
    name: ["", [Validators.required]],
    code: ["", [Validators.required]],
    description: [""],
    categoryId: [""],
    unitPrice: [0, [Validators.required, Validators.min(0.01)]],
    initialStock: [10, [Validators.min(0)]],
    additionalStock: [0, [Validators.min(0)]],
    minStock: [null, [Validators.min(0)]],
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes["productToEdit"]) {
      this.clearAiStatus();

      if (this.productToEdit) {
        let name = this.productToEdit.name || "";
        let desc = this.productToEdit.description || "";
        if (!name && desc.includes(" - ")) {
          const parts = desc.split(" - ");
          name = parts[0];
          desc = parts.slice(1).join(" - ");
        } else if (!name) {
          name = desc;
          desc = "";
        }

        this.form.patchValue({
          name: name,
          code: this.productToEdit.code,
          description: desc,
          categoryId: this.productToEdit.categoryId || "",
          unitPrice: this.productToEdit.unitPrice,
          additionalStock: 0,
          minStock: this.productToEdit.minStockQuantity ?? null,
        });
      } else {
        this.form.reset({
          name: "",
          code: "",
          description: "",
          categoryId: "",
          unitPrice: 0,
          initialStock: 10,
          additionalStock: 0,
          minStock: null,
        });
      }
    }
  }

  /**
   * Aciona a geração de descrição de produto com o microsserviço de IA
   */
  generateAiDescription(): void {
    const productName = this.form.get("name")?.value?.trim();
    if (!productName) {
      this.form.get("name")?.markAsTouched();
      this.aiErrorMessage =
        "Informe o nome do produto acima antes de gerar a descrição com IA.";
      return;
    }

    this.clearAiStatus();
    this.isGeneratingAi = true;

    // Resgata nome da categoria selecionada
    const categoryId = this.form.get("categoryId")?.value;
    const categoryObj = this.categories.find((c) => c.id === categoryId);
    const categoryName = categoryObj ? categoryObj.name : undefined;
    const currentDesc =
      this.form.get("description")?.value?.trim() || undefined;

    this.aiService
      .generateProductDescription({
        productName,
        categoryName,
        descriptionHint: currentDesc,
        tone: Number(this.selectedTone),
        language: "pt-BR",
        maxCharacters: 500,
      })
      .subscribe({
        next: (res) => {
          this.isGeneratingAi = false;
          if (res.isSuccess && res.generatedContent) {
            this.form.patchValue({ description: res.generatedContent });
            this.aiSuccessMessage = `Descrição gerada com sucesso via ${res.modelUsed}!`;

            setTimeout(() => {
              if (this.aiSuccessMessage?.includes(res.modelUsed)) {
                this.aiSuccessMessage = null;
              }
            }, 6000);
          } else {
            this.aiErrorMessage =
              res.errorMessage || "Não foi possível gerar a descrição pela IA.";
          }
        },
        error: (err) => {
          this.isGeneratingAi = false;
          this.aiErrorMessage =
            err.error?.detail ||
            err.error?.error ||
            "Erro de conexão com o microsserviço de IA.";
        },
      });
  }

  clearAiStatus(): void {
    this.aiSuccessMessage = null;
    this.aiErrorMessage = null;
  }

  onSubmit() {
    if (this.form.invalid) return;

    const val = this.form.value;

    if (this.productToEdit) {
      const updateDto: UpdateProductDto = {
        id: this.productToEdit.id,
        name: val.name,
        code: val.code,
        description: val.description || undefined,
        categoryId: val.categoryId || undefined,
        unitPrice: val.unitPrice,
        minStock: val.minStock != null ? val.minStock : undefined,
      };
      this.saveUpdate.emit({
        id: this.productToEdit.id,
        dto: updateDto,
        additionalStock: Number(val.additionalStock) || 0,
      } as any);
    } else {
      const createDto: CreateProductDto = {
        name: val.name,
        code: val.code,
        description: val.description || undefined,
        categoryId: val.categoryId || undefined,
        unitPrice: val.unitPrice,
        initialStock: val.initialStock ?? 0,
        minStock: val.minStock != null ? val.minStock : undefined,
      };
      this.saveCreate.emit(createDto);
    }
  }

  onClose() {
    this.clearAiStatus();
    this.close.emit();
  }
}
