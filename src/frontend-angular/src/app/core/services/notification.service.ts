import { Injectable } from "@angular/core";
import { HttpErrorResponse } from "@angular/common/http";
import { BehaviorSubject, Observable } from "rxjs";

export type ToastType = "success" | "error" | "warning" | "info";

export interface ToastMessage {
  id: string;
  type: ToastType;
  title: string;
  message: string;
  duration?: number;
}

@Injectable({
  providedIn: "root",
})
export class NotificationService {
  private toastsSubject = new BehaviorSubject<ToastMessage[]>([]);
  public toasts$: Observable<ToastMessage[]> =
    this.toastsSubject.asObservable();

  show(toast: Omit<ToastMessage, "id">): void {
    const id = Math.random().toString(36).substring(2, 9);
    const duration = toast.duration ?? 5000;
    const newToast: ToastMessage = { ...toast, id, duration };

    const current = this.toastsSubject.getValue();
    this.toastsSubject.next([...current, newToast]);

    if (duration > 0) {
      setTimeout(() => {
        this.remove(id);
      }, duration);
    }
  }

  success(title: string, message: string): void {
    this.show({ type: "success", title, message });
  }

  error(title: string, message: string): void {
    this.show({ type: "error", title, message });
  }

  warning(title: string, message: string): void {
    this.show({ type: "warning", title, message });
  }

  info(title: string, message: string): void {
    this.show({ type: "info", title, message });
  }

  handleHttpError(
    err: HttpErrorResponse,
    defaultTitle = "Erro de Comunicação",
  ): void {
    if (err.status === 409) {
      const conflictMsg =
        typeof err.error === "string"
          ? err.error
          : err.error?.detail ||
            err.error?.message ||
            "Conflito de concorrência ou estoque insuficiente.";
      this.error("Conflito de Estoque (409)", conflictMsg);
    } else if (err.status === 400) {
      const badReqMsg =
        typeof err.error === "string"
          ? err.error
          : err.error?.detail ||
            err.error?.message ||
            "Requisição inválida. Verifique os dados fornecidos.";
      this.warning("Requisição Inválida (400)", badReqMsg);
    } else if (err.status === 404) {
      this.warning(
        "Não Encontrado (404)",
        "O recurso solicitado não foi localizado no servidor.",
      );
    } else if (err.status === 500) {
      this.error(
        "Erro de Servidor (500)",
        "Ocorreu um erro interno no servidor.",
      );
    } else if (err.status === 0) {
      this.error(
        "Falha de Conectividade",
        "Não foi possível conectar ao servidor.",
      );
    } else {
      const genericMsg =
        err.error?.message ||
        err.message ||
        "Erro inesperado ao processar requisição.";
      this.error(defaultTitle, genericMsg);
    }
  }

  remove(id: string): void {
    const current = this.toastsSubject.getValue();
    this.toastsSubject.next(current.filter((t) => t.id !== id));
  }
}
