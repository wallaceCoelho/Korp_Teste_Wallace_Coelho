export enum InvoiceStatus {
  Open = 1,
  Closed = 2,
  Rejected = 3,
  Canceled = 4,
  Pending = 5,
  // Alias for backward compatibility if any
  Aberta = 1,
  Fechada = 2,
}

export interface InvoiceItem {
  id: string;
  productId: string;
  productCode: string;
  productDescription?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface Invoice {
  id: string;
  number: number;
  status: InvoiceStatus;
  statusDescription: string;
  reasonRejected?: string | null;
  totalAmount: number;
  createdAt: string;
  updatedAt?: string;
  printedAt?: string;
  items: InvoiceItem[];
}

export interface InvoiceProductOption {
  id: string;
  code: string;
  name?: string;
  description?: string;
  stockQuantity: number;
  unitPrice: number;
}

export interface CreateInvoiceItemDto {
  productId: string;
  productCode: string;
  productDescription: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateInvoiceDto {
  items: CreateInvoiceItemDto[];
}

export interface ListInvoicesResponse {
  items: Invoice[];
  totalCount: number;
}
