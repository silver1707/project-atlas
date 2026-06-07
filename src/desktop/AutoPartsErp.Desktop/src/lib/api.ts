import { getAccessToken, tenantHeaders } from "./auth";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";
export async function apiGet<T>(path: string): Promise<T> {
  return apiFetch<T>(path, { method: "GET" });
}

export async function apiPost<T>(path: string, body: unknown): Promise<T> {
  return apiFetch<T>(path, {
    method: "POST",
    body: JSON.stringify(body)
  });
}

async function apiFetch<T>(path: string, init: RequestInit): Promise<T> {
  const token = await getAccessToken();
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...tenantHeaders(),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers
    }
  });

  if (response.status === 401) {
    throw new Error("Sessao expirada ou login pendente.");
  }

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `HTTP ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export async function getLocalApiHealth(): Promise<"online" | "offline"> {
  try {
    const response = await fetch(`${apiBaseUrl}/health/ready`, { cache: "no-store" });
    return response.ok ? "online" : "offline";
  } catch {
    return "offline";
  }
}

export type OperationsDashboard = {
  salesCountToday: number;
  salesAmountToday: number;
  authorizedFiscalDocuments: number;
  rejectedFiscalDocuments: number;
  lowStockItems: number;
  pendingPickLists: number;
};

export type ProductSearchResult = {
  id: string;
  sku: string;
  description: string;
  brand: string;
  manufacturerCode: string;
  oeCode: string;
  gtin: string;
  suggestedPrice: number;
  equivalents: string[];
  applications: string[];
};

export type StockBalance = {
  productId: string;
  sku: string;
  description: string;
  locationId: string;
  location: string;
  onHand: number;
  reserved: number;
  available: number;
  minimum: number;
};

export type FiscalDocument = {
  id: string;
  model: string;
  kind: string;
  status: string;
  accessKey: string;
  number: string;
  series: string;
  protocol?: string;
  xmlHash: string;
  issuedAt: string;
};
