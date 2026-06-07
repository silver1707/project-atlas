import { useEffect, useState } from "react";
import { QueryClient, QueryClientProvider, useMutation } from "@tanstack/react-query";
import { DatabaseZap, Settings, WalletCards } from "lucide-react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { Shell, type Screen } from "./components/Shell";
import { CatalogSearch } from "./features/catalog/CatalogSearch";
import { DashboardPanel } from "./features/dashboards/DashboardPanel";
import { FiscalPanel } from "./features/fiscal/FiscalPanel";
import { InventoryPanel } from "./features/inventory/InventoryPanel";
import { SalesCounter } from "./features/sales/SalesCounter";
import { bootstrapAdmin, getBootstrapStatus, getSession, login, signOut } from "./lib/auth";
import { apiGet, getLocalApiHealth } from "./lib/api";
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
      const session = await getSession();
      setAuthenticated(Boolean(session));
      setAuthReady(true);
    }

    boot().catch(() => setAuthReady(true));
  }, []);

  if (!authReady) {
    return <div className="boot-screen">AutoParts ERP</div>;
  }

  return (
    <Shell
      active={active}
      onNavigate={setActive}
      isAuthenticated={authenticated}
      onSignOut={async () => {
        await signOut();
        setAuthenticated(false);
      }}
    >
      {!authenticated ? <LoginPanel onAuthenticated={() => setAuthenticated(true)} /> : <ScreenContent screen={active} />}
    </Shell>
  );
}

const loginSchema = z.object({
  username: z.string().min(3, "Informe o usuario"),
  password: z.string().min(8, "Informe a senha"),
  mfaCode: z.string().optional()
});

const bootstrapSchema = z.object({
  companyId: z.string().uuid(),
  branchId: z.string().uuid(),
  username: z.string().min(3),
  displayName: z.string().min(3),
  email: z.string().email(),
  password: z.string().min(12).regex(/[A-Z]/).regex(/[a-z]/).regex(/[0-9]/).regex(/[^A-Za-z0-9]/)
});

type LoginForm = z.infer<typeof loginSchema>;
type BootstrapForm = z.infer<typeof bootstrapSchema>;

function LoginPanel({ onAuthenticated }: { onAuthenticated: () => void }) {
  const health = useQuery({
    queryKey: ["local-api-health"],
    queryFn: getLocalApiHealth,
    refetchInterval: 5000
  });
  const bootstrap = useQuery({
    queryKey: ["bootstrap-status"],
    queryFn: getBootstrapStatus,
    enabled: health.data === "online"
  });

  const loginForm = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
    defaultValues: { username: "admin", password: "Admin@123456", mfaCode: "" }
  });
  const bootstrapForm = useForm<BootstrapForm>({
    resolver: zodResolver(bootstrapSchema),
    defaultValues: {
      companyId: "11111111-1111-1111-1111-111111111111",
      branchId: "22222222-2222-2222-2222-222222222222",
      username: "admin",
      displayName: "Administrador Local",
      email: "admin@autopartserp.local",
      password: "Admin@123456"
    }
  });

  const loginMutation = useMutation({
    mutationFn: login,
    onSuccess: onAuthenticated
  });
  const bootstrapMutation = useMutation({
    mutationFn: bootstrapAdmin,
    onSuccess: onAuthenticated
  });

  const needsBootstrap = bootstrap.data?.required;

  return (
    <div className="desktop-login">
      <div className="login-panel">
        <img src="/atlas-icon.svg" alt="" />
        <div>
          <h2>{needsBootstrap ? "Setup inicial" : "Acesso local"}</h2>
          <p>{needsBootstrap ? "Crie o administrador da loja." : "Entre no servidor local do ERP."}</p>
          <small className={health.data === "online" ? "success-text" : "error-text"}>
            Servidor local: {health.data === "online" ? "online" : "offline"}
          </small>
        </div>
      </div>

      {needsBootstrap ? (
        <form className="login-form" onSubmit={bootstrapForm.handleSubmit((values) => bootstrapMutation.mutate(values))}>
          <input {...bootstrapForm.register("companyId")} placeholder="Empresa ID" />
          <input {...bootstrapForm.register("branchId")} placeholder="Filial ID" />
          <input {...bootstrapForm.register("username")} placeholder="Usuario admin" />
          <input {...bootstrapForm.register("displayName")} placeholder="Nome" />
          <input {...bootstrapForm.register("email")} placeholder="Email" />
          <input {...bootstrapForm.register("password")} type="password" placeholder="Senha forte" />
          <button className="primary-button wide" disabled={bootstrapMutation.isPending || health.data !== "online"} type="submit">
            <span>Criar administrador</span>
          </button>
          {bootstrapMutation.error && <small className="error-text">{(bootstrapMutation.error as Error).message}</small>}
        </form>
      ) : (
        <form className="login-form" onSubmit={loginForm.handleSubmit((values) => loginMutation.mutate(values))}>
          <input {...loginForm.register("username")} placeholder="Usuario" />
          <input {...loginForm.register("password")} type="password" placeholder="Senha" />
          <input {...loginForm.register("mfaCode")} placeholder="Codigo MFA quando solicitado" />
          <button className="primary-button wide" disabled={loginMutation.isPending || health.data !== "online"} type="submit">
            <span>Entrar</span>
          </button>
          {loginMutation.error && <small className="error-text">{(loginMutation.error as Error).message}</small>}
        </form>
      )}

      <div className="service-panel">
        <DatabaseZap size={20} />
        <div>
          <strong>Modo desktop local</strong>
          <span>Configure este terminal para `localhost` ou para o servidor da loja na rede local.</span>
        </div>
      </div>
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
