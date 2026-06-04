import type { ReactNode } from "react";

type KpiCardProps = {
  title: string;
  value: string;
  tone?: "neutral" | "good" | "warn" | "danger";
  icon: ReactNode;
};

export function KpiCard({ title, value, tone = "neutral", icon }: KpiCardProps) {
  return (
    <div className={`kpi ${tone}`}>
      <div className="kpi-icon">{icon}</div>
      <div>
        <span>{title}</span>
        <strong>{value}</strong>
      </div>
    </div>
  );
}
