import { useEffect, useState } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { Settings, WalletCards } from "lucide-react";
import { Shell, type Screen } from "./components/Shell";
import { CatalogSearch } from "./features/catalog/CatalogSearch";
import { DashboardPanel } from "./features/dashboards/DashboardPanel";
import { FiscalPanel } from "./features/fiscal/FiscalPanel";
import { InventoryPanel } from "./features/inventory/InventoryPanel";
import { SalesCounter } from "./features/sales/SalesCounter";
import { completeSignIn, getUser, signIn } from "./lib/auth";
import { apiGet } from "./lib/api";
import { useQuery } from "@tanstack/react-query";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 20_000,
      retry: 1
    }
  }
});

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AtlasApp />
    </QueryClientProvider>
  );
}

function AtlasApp() {
  const [active, setActive] = useState<Screen>("dashboard");
  const [authenticated, setAuthenticated] = useState(false);
  const [authReady, setAuthReady] = useState(false);

  useEffect(() => {
    async function boot() {
      if (window.location.pathname === "/auth/callback") {
        await completeSignIn();
        window.history.replaceState({}, document.title, "/");
      }

      const user = await getUser();
      setAuthenticated(Boolean(user && !user.expired));
      setAuthReady(true);
    }

    boot().catch(() => setAuthReady(true));
  }, []);

  if (!authReady) {
    return <div className="boot-screen">Atlas ERP</div>;
  }

  return (
    <Shell active={active} onNavigate={setActive} isAuthenticated={authenticated}>
      {!authenticated ? <LoginPanel /> : <ScreenContent screen={active} />}
    </Shell>
  );
}

function LoginPanel() {
  return (
    <div className="login-panel">
      <img src="/atlas-icon.svg" alt="" />
      <div>
        <h2>Acesso seguro</h2>
        <p>Use o provedor de identidade da empresa.</p>
      </div>
      <button className="primary-button" onClick={() => signIn()} type="button">
        <span>Entrar</span>
      </button>
    </div>
  );
}

function ScreenContent({ screen }: { screen: Screen }) {
  switch (screen) {
    case "dashboard":
      return <DashboardPanel />;
    case "catalog":
      return <CatalogSearch />;
    case "sales":
      return <SalesCounter />;
    case "inventory":
      return <InventoryPanel />;
    case "fiscal":
      return <FiscalPanel />;
    case "finance":
      return <FinancePanel />;
    case "admin":
      return <AdminPanel />;
  }
}

function FinancePanel() {
  const { data, error, isLoading } = useQuery({
    queryKey: ["finance-dashboard"],
    queryFn: () =>
      apiGet<{
        overduePayables: number;
        overdueReceivables: number;
        openPayables: number;
        openReceivables: number;
      }>("/api/finance/dashboard")
  });

  if (isLoading) return <div className="loading-panel">Carregando financeiro...</div>;
  if (error) return <div className="alert-panel">{(error as Error).message}</div>;

  return (
    <div className="finance-board">
      <div className="finance-tile">
        <WalletCards size={24} />
        <span>A pagar aberto</span>
        <strong>{formatMoney(data?.openPayables ?? 0)}</strong>
      </div>
      <div className="finance-tile">
        <WalletCards size={24} />
        <span>A receber aberto</span>
        <strong>{formatMoney(data?.openReceivables ?? 0)}</strong>
      </div>
      <div className="finance-tile warn">
        <WalletCards size={24} />
        <span>A pagar vencido</span>
        <strong>{formatMoney(data?.overduePayables ?? 0)}</strong>
      </div>
      <div className="finance-tile danger">
        <WalletCards size={24} />
        <span>A receber vencido</span>
        <strong>{formatMoney(data?.overdueReceivables ?? 0)}</strong>
      </div>
    </div>
  );
}

function AdminPanel() {
  return (
    <div className="admin-grid">
      {["Empresas", "Filiais", "Perfis", "Tabelas fiscais", "Adaptadores", "Auditoria"].map((item) => (
        <button className="admin-tile" key={item} type="button">
          <Settings size={22} />
          <span>{item}</span>
        </button>
      ))}
    </div>
  );
}

function formatMoney(value: number) {
  return new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" }).format(value);
}
