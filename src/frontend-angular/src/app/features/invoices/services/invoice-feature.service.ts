import { Injectable, inject } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable, map } from "rxjs";
import { environment } from "../../../../environments/environment";
import {
  Invoice,
  InvoiceStatus,
  CreateInvoiceDto,
  ListInvoicesResponse,
  InvoiceProductOption,
} from "../models/invoice.models";

@Injectable({
  providedIn: "root",
})
export class InvoiceFeatureService {
  private http = inject(HttpClient);
  private invoicesApiUrl = `${environment.apiUrl}/invoices`;
  private productsApiUrl = `${environment.apiUrl}/products`;

  getInvoices(search?: string): Observable<ListInvoicesResponse> {
    let params = new HttpParams();
    if (search && search.trim()) {
      params = params.set("search", search.trim());
    }
    return this.http.get<ListInvoicesResponse>(this.invoicesApiUrl, { params });
  }

  getInvoiceById(id: string): Observable<Invoice> {
    return this.http.get<Invoice>(`${this.invoicesApiUrl}/${id}`);
  }

  createInvoice(dto: CreateInvoiceDto): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.invoicesApiUrl, dto);
  }

  updateInvoice(id: string, dto: CreateInvoiceDto): Observable<{ id: string }> {
    return this.http.put<{ id: string }>(`${this.invoicesApiUrl}/${id}`, dto.items);
  }

  printInvoice(id: string): Observable<void> {
    return this.http.post<void>(`${this.invoicesApiUrl}/${id}/print`, {});
  }

  cancelInvoice(id: string): Observable<void> {
    return this.http.post<void>(`${this.invoicesApiUrl}/${id}/cancel`, {});
  }

  listenInvoiceEvents(invoiceId: string): Observable<Invoice> {
    return new Observable<Invoice>((observer) => {
      const url = `${this.invoicesApiUrl}/${invoiceId}/events`;
      const eventSource = new EventSource(url);

      eventSource.onmessage = (event) => {
        try {
          const invoice: Invoice = JSON.parse(event.data);
          observer.next(invoice);
          if (invoice.status && invoice.status !== InvoiceStatus.Pending) {
            eventSource.close();
            observer.complete();
          }
        } catch (err) {
          observer.error(err);
          eventSource.close();
        }
      };

      eventSource.onerror = (err) => {
        if (eventSource.readyState === EventSource.CLOSED) {
          observer.complete();
        } else {
          observer.error(err);
          eventSource.close();
        }
      };

      return () => {
        eventSource.close();
      };
    });
  }

  getAvailableProducts(): Observable<InvoiceProductOption[]> {
    return this.http.get<{ items: any[] }>(this.productsApiUrl).pipe(
      map((res) =>
        (res.items || []).map((p) => ({
          id: p.id,
          code: p.code,
          name: p.name,
          description: p.description || p.name,
          stockQuantity: p.stockQuantity,
          unitPrice: p.unitPrice,
        })),
      ),
    );
  }
}
