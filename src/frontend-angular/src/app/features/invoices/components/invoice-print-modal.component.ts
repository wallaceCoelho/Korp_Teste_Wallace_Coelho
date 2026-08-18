import {
  Component,
  EventEmitter,
  Input,
  Output,
  OnChanges,
  SimpleChanges,
} from "@angular/core";
import { CommonModule, CurrencyPipe, DatePipe } from "@angular/common";
import { LucideAngularModule } from "lucide-angular";
import { Invoice, InvoiceItem, InvoiceProductOption, InvoiceStatus } from "../models/invoice.models";

@Component({
  selector: "app-invoice-print-modal",
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe, LucideAngularModule],
  templateUrl: "./invoice-print-modal.component.html",
})
export class InvoicePrintModalComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() invoice: Invoice | null = null;
  @Input() isDuplicate = false; // 2ª Via indicator
  @Input() autoPrint = false;
  @Input() products: InvoiceProductOption[] = [];

  @Output() close = new EventEmitter<void>();

  InvoiceStatus = InvoiceStatus;

  getProductName(item: InvoiceItem): string {
    if (this.products && this.products.length > 0) {
      const match = this.products.find(
        (p) => p.id === item.productId || p.code === item.productCode,
      );
      if (match?.name) return match.name;
    }
    if (item.productDescription) {
      if (item.productDescription.length <= 60 && !item.productDescription.includes(".")) {
        return item.productDescription;
      }
      const firstSentence = item.productDescription.split(".")[0].trim();
      if (firstSentence.length > 0 && firstSentence.length <= 60) {
        return firstSentence;
      }
      return item.productDescription.substring(0, 50).trim() + "...";
    }
    return item.productCode;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes["isOpen"] && this.isOpen && this.autoPrint && this.canPrint) {
      setTimeout(() => {
        this.triggerPrint();
      }, 300);
    }
  }

  get canPrint(): boolean {
    return this.invoice?.status === InvoiceStatus.Closed;
  }

  get formattedNumber(): string {
    return (this.invoice?.number || 0).toString().padStart(5, "0");
  }

  get accessKey(): string {
    if (!this.invoice?.id)
      return "35260812345678000190550010000000011000000000";
    const cleanId = this.invoice.id.replace(/-/g, "").toUpperCase();
    return `3526 0812 3456 7800 0190 5500 1${this.formattedNumber} 1${cleanId.substring(0, 8)} 0`;
  }

  triggerPrint() {
    if (!this.canPrint) return;

    const printElement = document.getElementById("invoice-print-sheet");
    if (!printElement) {
      window.print();
      return;
    }

    const printWindow = window.open(
      "",
      "_blank",
      "width=900,height=950,scrollbars=yes,resizable=yes",
    );
    if (!printWindow) {
      window.print();
      return;
    }

    // Copiar todos os estilos (Tailwind, fontes e CSS do Angular) para renderização 100% idêntica ao preview
    const styles = Array.from(
      document.querySelectorAll('style, link[rel="stylesheet"]'),
    )
      .map((el) => el.outerHTML)
      .join("\n");

    printWindow.document.open();
    printWindow.document.write(`
      <!DOCTYPE html>
      <html lang="pt-BR" class="bg-white">
        <head>
          <title>Nota Fiscal #${this.formattedNumber} - Enterprise Commerce</title>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          ${styles}
          <style>
            @page {
              size: A4 portrait;
              margin: 10mm 15mm;
            }
            body {
              background: #ffffff !important;
              color: #0f172a !important;
              padding: 24px;
              -webkit-print-color-adjust: exact !important;
              print-color-adjust: exact !important;
            }
            .no-print-toolbar {
              margin-bottom: 24px;
              padding: 12px 20px;
              background: #f8fafc;
              border-radius: 12px;
              display: flex;
              align-items: center;
              justify-content: space-between;
              border: 1px solid #cbd5e1;
              font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            }
            .print-btn {
              padding: 9px 18px;
              border-radius: 8px;
              font-weight: 600;
              font-size: 13px;
              cursor: pointer;
              border: none;
              transition: all 0.15s ease-in-out;
              display: inline-flex;
              align-items: center;
              gap: 6px;
            }
            .btn-print {
              background: #059669;
              color: white;
            }
            .btn-print:hover {
              background: #047857;
            }
            .btn-close {
              background: #e2e8f0;
              color: #334155;
              margin-left: 8px;
            }
            .btn-close:hover {
              background: #cbd5e1;
            }
            @media print {
              .no-print-toolbar {
                display: none !important;
              }
              body {
                padding: 0 !important;
              }
            }
          </style>
        </head>
        <body class="bg-white text-slate-900 font-sans">
          <div class="no-print-toolbar">
            <div style="font-weight: 700; font-size: 14px; color: #0f172a;">
              Nota Fiscal Eletrônica #${this.formattedNumber}
            </div>
            <div>
              <button class="print-btn btn-print" onclick="window.print()">
                <span>Imprimir / Salvar em PDF</span>
              </button>
              <button class="print-btn btn-close" onclick="window.close()">
                <span>Fechar Janela</span>
              </button>
            </div>
          </div>

          <div style="max-width: 820px; margin: 0 auto;">
            ${printElement.outerHTML}
          </div>

          <script>
            window.addEventListener('load', () => {
              setTimeout(() => {
                window.print();
              }, 300);
            });
          </script>
        </body>
      </html>
    `);
    printWindow.document.close();
  }

  downloadDocument() {
    const printElement = document.getElementById("invoice-print-sheet");
    if (!printElement) return;

    const styles = Array.from(
      document.querySelectorAll('style, link[rel="stylesheet"]'),
    )
      .map((el) => el.outerHTML)
      .join("\n");

    const htmlContent = `
      <!DOCTYPE html>
      <html lang="pt-BR">
        <head>
          <title>Nota Fiscal #${this.formattedNumber}</title>
          <meta charset="utf-8">
          ${styles}
          <style>
            @page { size: A4 portrait; margin: 10mm 15mm; }
            body { background: #ffffff !important; color: #0f172a !important; padding: 20px; }
          </style>
        </head>
        <body class="bg-white text-slate-900 font-sans">
          <div style="max-width: 820px; margin: 0 auto;">
            ${printElement.outerHTML}
          </div>
        </body>
      </html>
    `;

    const blob = new Blob([htmlContent], { type: "text/html;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `NotaFiscal_${this.formattedNumber}.html`;
    a.click();
    URL.revokeObjectURL(url);
  }

  onClose() {
    this.close.emit();
  }
}
