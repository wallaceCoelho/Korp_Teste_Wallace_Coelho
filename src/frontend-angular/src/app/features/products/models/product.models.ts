export interface Product {
  id: string;
  code: string;
  name?: string;
  description: string;
  stockQuantity: number;
  minStockQuantity?: number;
  unitPrice: number;
  createdAt: string;
  updatedAt?: string;
  categoryId?: string;
  category?: {
    id: string;
    name: string;
  };
}

export interface CreateProductDto {
  code: string;
  name: string;
  description?: string;
  initialStock: number;
  minStock?: number;
  unitPrice: number;
  categoryId?: string;
}

export interface UpdateProductDto {
  id?: string;
  code: string;
  name: string;
  description?: string;
  unitPrice: number;
  minStock?: number;
  categoryId?: string;
}

export enum StockOperationType {
  Add = 1,
  Deduct = 2
}

export interface UpdateStockDto {
  productId?: string;
  quantity: number;
  operation: StockOperationType;
}

export interface ListProductsResponse {
  items: Product[];
  totalCount: number;
}

export interface ProductCategoryOption {
  id: string;
  name: string;
}
