import type { RequestStatus } from "@/lib/api/types";
import { statusLabels } from "@/lib/labels";

const styles: Record<RequestStatus, string> = {
  Pending: "bg-amber-100 text-amber-800 ring-amber-200",
  InProgress: "bg-blue-100 text-blue-800 ring-blue-200",
  OnHold: "bg-violet-100 text-violet-800 ring-violet-200",
  Resolved: "bg-emerald-100 text-emerald-800 ring-emerald-200",
  Cancelled: "bg-slate-200 text-slate-700 ring-slate-300",
};

export function StatusBadge({ status }: Readonly<{ status: RequestStatus }>) {
  return (
    <span
      className={`inline-flex items-center whitespace-nowrap rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${styles[status]}`}
    >
      {statusLabels[status]}
    </span>
  );
}
