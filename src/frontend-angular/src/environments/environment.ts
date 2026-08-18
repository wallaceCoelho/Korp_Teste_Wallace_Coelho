// Arquivo gerado automaticamente pelo script set-env.js
export const environment = {
  production: false,
  apiUrl: (typeof window !== 'undefined' && (window as any).env?.API_URL)
    ? (window as any).env.API_URL
    : 'http://localhost:5050/api'
};
