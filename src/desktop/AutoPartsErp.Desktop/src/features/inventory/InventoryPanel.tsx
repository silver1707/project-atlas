import { Boxes, PackageCheck, PackageX } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { KpiCard } from "../../components/KpiCard";
import { apiGet, type StockBalance } from "../../lib/api";

export function InventoryPanel() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["stock-balances"],
    queryFn: () => apiGet<StockBalance[]>("/api/inventory/balances")
  });

  const balances = data ?? [];
  const lowStock = balances.filter((item) => item.available < item.minimum).length;

  if (isLoading) {
    return <div className="loading-panel">Carregando estoque...</div>;
  }

  if (error) {
    return <div className="alert-panel">{(error as Error).message}</div>;
  }

  return (
    <div className="feature-stack">
      <div className="dashboard-grid compact">
        <KpiCard icon={<Boxes size={22} />} title="Itens com saldo" value={`${balances.length}`} />
        <KpiCard icon={<PackageCheck size={22} />} title="Disponivel total" value={`${balances.reduce((sum, item) => sum + item.available, 0)}`} tone="good" />
        <KpiCard icon={<PackageX size={22} />} title="Abaixo do minimo" value={`${lowStock}`} tone={lowStock > 0 ? "warn" : "good"} />
      </div>
      <div className="table-panel">
        <table>
          <thead>
            <tr>
              <th>SKU</th>
              <th>Descricao</th>
              <th>Local</th>
              <th>Fisico</th>
              <th>Reservado</th>
              <th>Disponivel</th>
              <th>Minimo</th>
            </tr>
          </thead>
          <tbody>
            {balances.map((item) => (
              <tr key={`${item.productId}-${item.locationId}`}>
                <td>{item.sku}</td>
                <td>{item.description}</td>
                <td>{item.location}</td>
                <td>{item.onHand}</td>
                <td>{item.reserved}</td>
                <td>{item.available}</td>
                <td>{item.minimum}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
