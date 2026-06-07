type StatusBadgeProps = {
  value: string;
};

const tones: Record<string, string> = {
  authorized: "good",
  pending_authorization: "warn",
  rejected: "danger",
  cancelled: "danger",
  open: "warn",
  closed: "neutral",
  ready_for_fiscal: "good",
  pending: "warn"
};

export function StatusBadge({ value }: StatusBadgeProps) {
  return <span className={`status ${tones[value] ?? "neutral"}`}>{value.replaceAll("_", " ")}</span>;
}
