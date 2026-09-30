import Link from "next/link";
import { PriorityBadge } from "@/components/ui/PriorityBadge";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { MaintenanceRequestListItem } from "@/lib/api/types";
import { formatDateTime } from "@/lib/format";
import { categoryLabels } from "@/lib/labels";

/** Mobile layout (below md): one card per request instead of a wide table. */
export function RequestCard({ request }: Readonly<{ request: MaintenanceRequestListItem }>) {
  return (
    <Link
      href={`/requests/${request.id}`}
      className="block rounded-lg border border-slate-200 bg-white p-4 hover:border-slate-300"
    >
      <div className="flex items-start justify-between gap-3">
        <p className="min-w-0 break-words font-medium text-slate-900">{request.title}</p>
        <StatusBadge status={request.status} />
      </div>
      <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-600">
        <PriorityBadge priority={request.priority} />
        <span>{categoryLabels[request.category]}</span>
        <span>{request.assignee?.name ?? "Sin asignar"}</span>
      </div>
      <p className="mt-2 text-xs text-slate-500">{formatDateTime(request.createdAt)}</p>
    </Link>
  );
}
