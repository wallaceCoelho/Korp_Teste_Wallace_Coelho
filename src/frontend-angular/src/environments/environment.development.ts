// Arquivo gerado automaticamente pelo script set-env.js
export const environment = {
  production: false,
  apiUrl: (typeof window !== 'undefined' && (window as any).env?.API_URL)
    ? (window as any).env.API_URL
    : 'https://korpapi.wcoelho.com.br/api',
  monitorUrl: (typeof window !== 'undefined' && (window as any).env?.MONITOR_URL)
    ? (window as any).env.MONITOR_URL
    : 'http://localhost:18888'
};
