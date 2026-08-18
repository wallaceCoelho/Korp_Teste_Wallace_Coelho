export interface Category {
  id: string;
  name: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateCategoryDto {
  name: string;
}

export interface UpdateCategoryDto {
  id?: string;
  name: string;
}

export interface ListCategoriesResponse {
  items: Category[];
  totalCount: number;
}
