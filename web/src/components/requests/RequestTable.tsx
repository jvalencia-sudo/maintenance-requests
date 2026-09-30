import Link from "next/link";
import { PriorityBadge } from "@/components/ui/PriorityBadge";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { MaintenanceRequestListItem } from "@/lib/api/types";
import { formatDateTime } from "@/lib/format";
import { categoryLabels } from "@/lib/labels";

/** Desktop layout (lg and up). Rows are rendered in the order the API returned them. */
export function RequestTable({ requests }: Readonly<{ requests: MaintenanceRequestListItem[] }>) {
  // Horizontal scroll as a safety net: a column is never clipped out of reach.
  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white">
      <table className="w-full text-left text-sm">
        <thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500">
          <tr>
            <th className="px-4 py-3 font-medium">#</th>
            <th className="px-4 py-3 font-medium">Título</th>
            <th className="px-4 py-3 font-medium">Estado</th>
            <th className="px-4 py-3 font-medium">Prioridad</th>
            <th className="px-4 py-3 font-medium">Categoría</th>
            <th className="px-4 py-3 font-medium">Responsable</th>
            <th className="px-4 py-3 font-medium">Creada</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {requests.map((request) => (
            <tr key={request.id} className="hover:bg-slate-50">
              <td className="whitespace-nowrap px-4 py-3 tabular-nums text-slate-500">{request.id}</td>
              <td className="max-w-xs px-4 py-3 wrap-anywhere">
                <Link href={`/requests/${request.id}`} className="font-medium text-blue-700 hover:underline">
                  {request.title}
                </Link>
              </td>
              <td className="px-4 py-3">
                <StatusBadge status={request.status} />
              </td>
              <td className="px-4 py-3">
                <PriorityBadge priority={request.priority} />
              </td>
              <td className="px-4 py-3 text-slate-700">{categoryLabels[request.category]}</td>
              <td className="px-4 py-3 text-slate-700">{request.assignee?.name ?? "Sin asignar"}</td>
              <td className="whitespace-nowrap px-4 py-3 text-slate-600">{formatDateTime(request.createdAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
