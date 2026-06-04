import { Search, SlidersHorizontal } from "lucide-react";
import { FormEvent, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { apiGet, type ProductSearchResult } from "../../lib/api";

const money = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

export function CatalogSearch() {
  const [term, setTerm] = useState("");
  const [make, setMake] = useState("");
  const [model, setModel] = useState("");
  const [year, setYear] = useState("");
  const [query, setQuery] = useState("term=");

  const { data, isFetching, error } = useQuery({
    queryKey: ["catalog-search", query],
    queryFn: () => apiGet<ProductSearchResult[]>(`/api/catalog/products/search?${query}`),
    enabled: query.length > 5
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    const params = new URLSearchParams();
    if (term) params.set("term", term);
    if (make) params.set("make", make);
    if (model) params.set("model", model);
    if (year) params.set("year", year);
    params.set("limit", "60");
    setQuery(params.toString());
  }

  return (
    <div className="feature-stack">
      <form className="tool-panel search-panel" onSubmit={submit}>
        <label>
          <span>Codigo, OE, GTIN ou descricao</span>
          <input value={term} onChange={(event) => setTerm(event.target.value)} placeholder="NKF1234, 04465-0K290, pastilha..." />
        </label>
        <label>
          <span>Marca veiculo</span>
          <input value={make} onChange={(event) => setMake(event.target.value)} placeholder="Toyota" />
        </label>
        <label>
          <span>Modelo</span>
          <input value={model} onChange={(event) => setModel(event.target.value)} placeholder="Corolla" />
        </label>
        <label>
          <span>Ano</span>
          <input value={year} onChange={(event) => setYear(event.target.value)} inputMode="numeric" placeholder="2018" />
        </label>
        <button className="primary-button" type="submit">
          <Search size={18} />
          <span>Buscar</span>
        </button>
        <button className="icon-button" type="button" title="Filtros tecnicos">
          <SlidersHorizontal size={18} />
        </button>
      </form>

      {isFetching && <div className="loading-panel">Buscando catalogo...</div>}
      {error && <div className="alert-panel">{(error as Error).message}</div>}

      <div className="product-results">
        {(data ?? []).map((product) => (
          <article className="product-row" key={product.id}>
            <div>
              <strong>{product.sku}</strong>
              <span>{product.description}</span>
              <small>
                {product.brand} | Fab. {product.manufacturerCode} | OE {product.oeCode || "-"} | GTIN {product.gtin || "-"}
              </small>
            </div>
            <div className="fitment-list">
              {product.applications.map((application) => (
                <span key={application}>{application}</span>
              ))}
            </div>
            <div className="equiv-list">
              {product.equivalents.map((equivalent) => (
                <span key={equivalent}>{equivalent}</span>
              ))}
            </div>
            <strong className="price">{money.format(product.suggestedPrice)}</strong>
          </article>
        ))}
      </div>
    </div>
  );
}
