import { Injectable, inject } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable, map } from "rxjs";
import { environment } from "../../../../environments/environment";
import {
  Product,
  CreateProductDto,
  UpdateProductDto,
  ListProductsResponse,
  ProductCategoryOption,
  StockOperationType,
  UpdateStockDto,
} from "../models/product.models";

@Injectable({
  providedIn: "root",
})
export class ProductFeatureService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/products`;
  private categoriesApiUrl = `${environment.apiUrl}/categories`;

  getProducts(search?: string, categoryId?: string): Observable<ListProductsResponse> {
    let params = new HttpParams();
    if (search && search.trim()) {
      params = params.set("search", search.trim());
    }
    if (categoryId && categoryId !== "ALL") {
      params = params.set("categoryId", categoryId);
    }
    return this.http.get<ListProductsResponse>(this.apiUrl, { params });
  }

  getProductById(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  createProduct(dto: CreateProductDto): Observable<{ id: string }> {
    const payload = {
      code: dto.code,
      name: dto.name,
      description: dto.description || null,
      initialStock: dto.initialStock,
      minStock: dto.minStock ?? null,
      unitPrice: dto.unitPrice,
      categoryId: dto.categoryId || null,
    };
    return this.http.post<{ id: string }>(this.apiUrl, payload);
  }

  updateProduct(id: string, dto: UpdateProductDto): Observable<void> {
    const payload = {
      id: id,
      code: dto.code,
      name: dto.name,
      description: dto.description || null,
      unitPrice: dto.unitPrice,
      minStock: dto.minStock ?? null,
      categoryId: dto.categoryId || null,
    };
    return this.http.put<void>(`${this.apiUrl}/${id}`, payload);
  }

  updateStock(id: string, quantity: number, operation: StockOperationType = StockOperationType.Add): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/stock`, {
      productId: id,
      quantity: Math.abs(quantity),
      operation: operation
    });
  }

  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getCategories(): Observable<ProductCategoryOption[]> {
    return this.http
      .get<any>(this.categoriesApiUrl)
      .pipe(
        map((res) => {
          const items = Array.isArray(res) ? res : (res?.items || []);
          return items.map((c: any) => ({ id: c.id, name: c.name }));
        }),
      );
  }
}
