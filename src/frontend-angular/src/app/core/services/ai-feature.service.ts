import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export enum AiToneType {
  Commercial = 1,
  Technical = 2,
  Persuasive = 3,
  Minimalist = 4,
  Casual = 5
}

export interface ProductDescriptionPayload {
  productName: string;
  categoryName?: string;
  descriptionHint?: string;
  tone?: AiToneType;
  language?: string;
  maxCharacters?: number;
}

export interface AiTaskResponse {
  requestId: string;
  featureType: number;
  generatedContent: string;
  modelUsed: string;
  providerUsed: number;
  executionDuration: string;
  completedAt: string;
  isSuccess: boolean;
  errorMessage?: string;
}

export interface AiFeaturesMetadata {
  activeProvider: string;
  activeModel: string;
  supportedFeatures: Array<{
    featureId: number;
    featureName: string;
    description: string;
  }>;
}

@Injectable({
  providedIn: 'root'
})
export class AiFeatureService {
  private readonly baseUrl = `${environment.apiUrl}/ai`;

  constructor(private http: HttpClient) {}

  /**
   * Gera uma descrição otimizada para produto usando IA.
   */
  generateProductDescription(payload: ProductDescriptionPayload): Observable<AiTaskResponse> {
    return this.http.post<AiTaskResponse>(`${this.baseUrl}/product-description`, payload);
  }

  /**
   * Consulta os metadados do serviço de IA (provedor, modelo ativo e recursos suportados).
   */
  getFeatures(): Observable<AiFeaturesMetadata> {
    return this.http.get<AiFeaturesMetadata>(`${this.baseUrl}/features`);
  }

  /**
   * Consulta o resultado de uma requisição de IA pelo identificador GUID.
   */
  getRequestById(requestId: string): Observable<AiTaskResponse> {
    return this.http.get<AiTaskResponse>(`${this.baseUrl}/requests/${requestId}`);
  }
}
