import { AlertTriangle, CheckCircle2, ClipboardList, PackageMinus, Receipt, ShoppingBag } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { apiGet, type OperationsDashboard } from "../../lib/api";
import { KpiCard } from "../../components/KpiCard";

const money = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

export function DashboardPanel() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["operations-dashboard"],
    queryFn: () => apiGet<OperationsDashboard>("/api/reports/operations-dashboard")
  });

  if (isLoading) {
    return <div className="loading-panel">Carregando painel operacional...</div>;
  }

  if (error) {
    return <div className="alert-panel">{(error as Error).message}</div>;
  }

  return (
    <div className="dashboard-grid">
      <KpiCard icon={<ShoppingBag size={22} />} title="Vendas hoje" value={`${data?.salesCountToday ?? 0}`} tone="good" />
      <KpiCard icon={<Receipt size={22} />} title="Faturamento" value={money.format(data?.salesAmountToday ?? 0)} />
      <KpiCard icon={<CheckCircle2 size={22} />} title="DF-e autorizados" value={`${data?.authorizedFiscalDocuments ?? 0}`} tone="good" />
      <KpiCard icon={<AlertTriangle size={22} />} title="Rejeicoes fiscais" value={`${data?.rejectedFiscalDocuments ?? 0}`} tone="danger" />
      <KpiCard icon={<PackageMinus size={22} />} title="Estoque baixo" value={`${data?.lowStockItems ?? 0}`} tone="warn" />
      <KpiCard icon={<ClipboardList size={22} />} title="Pickings pendentes" value={`${data?.pendingPickLists ?? 0}`} tone="warn" />
    </div>
  );
}
