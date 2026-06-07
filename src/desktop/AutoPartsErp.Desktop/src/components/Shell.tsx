import {
  BarChart3,
  Boxes,
  Building2,
  Calculator,
  FileDigit,
  LogIn,
  LogOut,
  PackageSearch,
  ShieldCheck,
  ShoppingCart
} from "lucide-react";
import type { ReactNode } from "react";

export type Screen = "dashboard" | "catalog" | "sales" | "inventory" | "fiscal" | "finance" | "admin";

type ShellProps = {
  active: Screen;
  onNavigate: (screen: Screen) => void;
  isAuthenticated: boolean;
  onSignOut: () => void;
  children: ReactNode;
};

const nav = [
  { id: "dashboard", label: "Painel", icon: BarChart3 },
  { id: "catalog", label: "Catalogo", icon: PackageSearch },
  { id: "sales", label: "Balcao", icon: ShoppingCart },
  { id: "inventory", label: "Estoque", icon: Boxes },
  { id: "fiscal", label: "Fiscal", icon: FileDigit },
  { id: "finance", label: "Financeiro", icon: Calculator },
  { id: "admin", label: "Admin", icon: Building2 }
] satisfies Array<{ id: Screen; label: string; icon: typeof BarChart3 }>;

export function Shell({ active, onNavigate, isAuthenticated, onSignOut, children }: ShellProps) {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <img src="/atlas-icon.svg" alt="" />
          <div>
            <strong>AutoParts ERP</strong>
            <span>Desktop Local</span>
          </div>
        </div>
        <nav aria-label="Modulo">
          {nav.map((item) => {
            const Icon = item.icon;
            return (
              <button
                key={item.id}
                className={active === item.id ? "nav-item active" : "nav-item"}
                onClick={() => onNavigate(item.id)}
                type="button"
                title={item.label}
              >
                <Icon size={20} />
                <span>{item.label}</span>
              </button>
            );
          })}
        </nav>
        <div className="sidebar-footer">
          <ShieldCheck size={18} />
          <span>Login local + RBAC</span>
        </div>
      </aside>
      <main>
        <header className="topbar">
          <div>
            <span className="eyebrow">Matriz Sao Paulo</span>
            <h1>{nav.find((item) => item.id === active)?.label}</h1>
          </div>
          <button className="icon-text-button" onClick={() => (isAuthenticated ? onSignOut() : undefined)} type="button">
            {isAuthenticated ? <LogOut size={18} /> : <LogIn size={18} />}
            <span>{isAuthenticated ? "Sair" : "Entrar"}</span>
          </button>
        </header>
        <section className="workspace">{children}</section>
      </main>
    </div>
  );
}
