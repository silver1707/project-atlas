import { FormEvent, useMemo, useState } from "react";
import { Plus, Search, Trash2 } from "lucide-react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { apiGet, apiPost, type ProductSearchResult, type StockBalance } from "../../lib/api";

const money = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

type CartLine = ProductSearchResult & {
  quantity: number;
  locationId: string;
};

export function SalesCounter() {
  const [term, setTerm] = useState("");
  const [search, setSearch] = useState("");
  const [cart, setCart] = useState<CartLine[]>([]);

  const products = useQuery({
    queryKey: ["counter-products", search],
    queryFn: () => apiGet<ProductSearchResult[]>(`/api/catalog/products/search?term=${encodeURIComponent(search)}&limit=20`),
    enabled: search.length >= 2
  });

  const balances = useQuery({
    queryKey: ["counter-balances"],
    queryFn: () => apiGet<StockBalance[]>("/api/inventory/balances")
  });

  const defaultLocationByProduct = useMemo(() => {
    const map = new Map<string, string>();
    for (const balance of balances.data ?? []) {
      if (balance.available > 0 && !map.has(balance.productId)) {
        map.set(balance.productId, balance.locationId);
      }
    }
    return map;
  }, [balances.data]);

  const total = cart.reduce((sum, line) => sum + line.quantity * line.suggestedPrice, 0);

  const sale = useMutation({
    mutationFn: () =>
      apiPost("/api/sales/counter-sales", {
        customerId: null,
        priceTable: "varejo",
        paymentTerms: "a vista",
        salespersonId: "balcao",
        commissionPercent: 0,
        lines: cart.map((line) => ({
          productId: line.id,
          locationId: line.locationId,
          quantity: line.quantity,
          unitPrice: line.suggestedPrice,
          discount: 0
        }))
      }),
    onSuccess: () => setCart([])
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    setSearch(term.trim());
  }

  function add(product: ProductSearchResult) {
    const locationId = defaultLocationByProduct.get(product.id);
    if (!locationId) {
      return;
    }

    setCart((current) => {
      const existing = current.find((line) => line.id === product.id);
      if (existing) {
        return current.map((line) => (line.id === product.id ? { ...line, quantity: line.quantity + 1 } : line));
      }

      return [...current, { ...product, quantity: 1, locationId }];
    });
  }

  return (
    <div className="sales-layout">
      <section className="tool-panel">
        <form className="counter-search" onSubmit={submit}>
          <input value={term} onChange={(event) => setTerm(event.target.value)} placeholder="Codigo, OE, descricao ou GTIN" />
          <button className="primary-button" type="submit">
            <Search size={18} />
            <span>Buscar</span>
          </button>
        </form>
        <div className="counter-results">
          {(products.data ?? []).map((product) => (
            <button className="counter-product" key={product.id} onClick={() => add(product)} type="button">
              <div>
                <strong>{product.sku}</strong>
                <span>{product.description}</span>
              </div>
              <span>{money.format(product.suggestedPrice)}</span>
              <Plus size={18} />
            </button>
          ))}
        </div>
      </section>

      <section className="checkout-panel">
        <div className="checkout-lines">
          {cart.map((line) => (
            <div className="checkout-line" key={line.id}>
              <div>
                <strong>{line.sku}</strong>
                <span>{line.description}</span>
              </div>
              <input
                aria-label={`Quantidade ${line.sku}`}
                value={line.quantity}
                min={1}
                type="number"
                onChange={(event) =>
                  setCart((current) =>
                    current.map((item) => (item.id === line.id ? { ...item, quantity: Number(event.target.value) } : item))
                  )
                }
              />
              <strong>{money.format(line.quantity * line.suggestedPrice)}</strong>
              <button className="icon-button" onClick={() => setCart((current) => current.filter((item) => item.id !== line.id))} type="button" title="Remover">
                <Trash2 size={18} />
              </button>
            </div>
          ))}
        </div>
        <footer className="checkout-total">
          <span>Total</span>
          <strong>{money.format(total)}</strong>
          <button className="primary-button wide" type="button" disabled={cart.length === 0 || sale.isPending} onClick={() => sale.mutate()}>
            <span>Fechar venda</span>
          </button>
          {sale.error && <small className="error-text">{(sale.error as Error).message}</small>}
          {sale.isSuccess && <small className="success-text">Venda registrada.</small>}
        </footer>
      </section>
    </div>
  );
}
