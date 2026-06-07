import { FileCheck2, FileClock, RefreshCcw } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { KpiCard } from "../../components/KpiCard";
import { StatusBadge } from "../../components/StatusBadge";
import { apiGet, type FiscalDocument } from "../../lib/api";

export function FiscalPanel() {
  const { data, isLoading, error, refetch, isFetching } = useQuery({
    queryKey: ["fiscal-documents"],
    queryFn: () => apiGet<FiscalDocument[]>("/api/fiscal/documents")
  });

  const documents = data ?? [];
  const authorized = documents.filter((doc) => doc.status === "authorized").length;
  const pending = documents.filter((doc) => doc.status === "pending_authorization").length;

  if (isLoading) {
    return <div className="loading-panel">Carregando documentos fiscais...</div>;
  }

  if (error) {
    return <div className="alert-panel">{(error as Error).message}</div>;
  }

  return (
    <div className="feature-stack">
      <div className="panel-toolbar">
        <div className="dashboard-grid compact">
          <KpiCard icon={<FileCheck2 size={22} />} title="Autorizados" value={`${authorized}`} tone="good" />
          <KpiCard icon={<FileClock size={22} />} title="Pendentes" value={`${pending}`} tone="warn" />
        </div>
        <button className="icon-text-button" type="button" onClick={() => refetch()} disabled={isFetching}>
          <RefreshCcw size={18} />
          <span>Atualizar</span>
        </button>
      </div>
      <div className="table-panel">
        <table>
          <thead>
            <tr>
              <th>Modelo</th>
              <th>Numero</th>
              <th>Status</th>
              <th>Chave</th>
              <th>Protocolo</th>
              <th>XML</th>
            </tr>
          </thead>
          <tbody>
            {documents.map((document) => (
              <tr key={document.id}>
                <td>{document.model}</td>
                <td>
                  {document.series}/{document.number}
                </td>
                <td>
                  <StatusBadge value={document.status} />
                </td>
                <td className="mono-cell">{document.accessKey || "-"}</td>
                <td className="mono-cell">{document.protocol || "-"}</td>
                <td className="mono-cell">{document.xmlHash.slice(0, 12)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
