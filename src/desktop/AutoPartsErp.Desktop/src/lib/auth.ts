import { invoke } from "@tauri-apps/api/core";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";
const storageKey = "autoparts-erp-local-session";

export type LocalSession = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  roles: string[];
};

export type LoginPayload = {
  username: string;
  password: string;
  mfaCode?: string;
};

export type BootstrapAdminPayload = {
  companyId: string;
  branchId: string;
  username: string;
  displayName: string;
  email: string;
  password: string;
};

function isTauriRuntime() {
  return "__TAURI_INTERNALS__" in window;
}

export async function getSession(): Promise<LocalSession | null> {
  try {
    if (isTauriRuntime()) {
      const value = await invoke<string | null>("load_session");
      return value ? JSON.parse(value) as LocalSession : null;
    }

    const value = window.sessionStorage.getItem(storageKey);
    return value ? JSON.parse(value) as LocalSession : null;
  } catch {
    return null;
  }
}

export async function saveSession(session: LocalSession): Promise<void> {
  if (isTauriRuntime()) {
    await invoke("save_session", {
      payload: JSON.stringify(session)
    });
  }

  window.sessionStorage.setItem(storageKey, JSON.stringify(session));
}

export async function getAccessToken(): Promise<string | null> {
  const session = await getSession();
  if (!session) {
    return null;
  }

  if (new Date(session.accessTokenExpiresAt) > new Date(Date.now() + 60_000)) {
    return session.accessToken;
  }

  const refreshed = await refreshSession(session.refreshToken);
  return refreshed.accessToken;
}

export async function login(payload: LoginPayload): Promise<LocalSession> {
  const response = await fetch(`${apiBaseUrl}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...tenantHeaders() },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    throw new Error("Usuario ou senha invalidos.");
  }

  const session = await response.json() as LocalSession;
  await saveSession(session);
  return session;
}

export async function bootstrapAdmin(payload: BootstrapAdminPayload): Promise<LocalSession> {
  const response = await fetch(`${apiBaseUrl}/api/auth/bootstrap/admin`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...tenantHeaders() },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  const session = await response.json() as LocalSession;
  await saveSession(session);
  return session;
}

export async function getBootstrapStatus(): Promise<{ required: boolean }> {
  const response = await fetch(`${apiBaseUrl}/api/auth/bootstrap/status`, {
    headers: tenantHeaders()
  });
  if (!response.ok) {
    throw new Error("Servidor local indisponivel.");
  }

  return response.json() as Promise<{ required: boolean }>;
}

export async function refreshSession(refreshToken: string): Promise<LocalSession> {
  const response = await fetch(`${apiBaseUrl}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...tenantHeaders() },
    body: JSON.stringify({ refreshToken })
  });

  if (!response.ok) {
    await signOut();
    throw new Error("Sessao expirada.");
  }

  const session = await response.json() as LocalSession;
  await saveSession(session);
  return session;
}

export async function signOut(): Promise<void> {
  const session = await getSession();
  if (session) {
    await fetch(`${apiBaseUrl}/api/auth/logout`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${session.accessToken}`,
        ...tenantHeaders()
      },
      body: JSON.stringify({ refreshToken: session.refreshToken })
    }).catch(() => undefined);
  }

  if (isTauriRuntime()) {
    await invoke("clear_session").catch(() => undefined);
  }

  window.sessionStorage.removeItem(storageKey);
}

export function tenantHeaders() {
  return {
    "X-Company-Id": import.meta.env.VITE_COMPANY_ID ?? "11111111-1111-1111-1111-111111111111",
    "X-Branch-Id": import.meta.env.VITE_BRANCH_ID ?? "22222222-2222-2222-2222-222222222222"
  };
}
