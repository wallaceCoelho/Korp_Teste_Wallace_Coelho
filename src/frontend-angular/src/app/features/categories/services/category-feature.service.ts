import { Injectable, inject } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";

import {
  Category,
  CreateCategoryDto,
  UpdateCategoryDto,
  ListCategoriesResponse,
} from "../models/category.models";

@Injectable({
  providedIn: "root",
})
export class CategoryFeatureService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/categories`;

  getCategories(search?: string): Observable<ListCategoriesResponse> {
    let params = new HttpParams();
    if (search && search.trim()) {
      params = params.set("search", search.trim());
    }
    return this.http.get<ListCategoriesResponse>(this.apiUrl, { params });
  }

  getCategoryById(id: string): Observable<Category> {
    return this.http.get<Category>(`${this.apiUrl}/${id}`);
  }

  createCategory(dto: CreateCategoryDto): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.apiUrl, dto);
  }

  updateCategory(id: string, dto: UpdateCategoryDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, dto);
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
