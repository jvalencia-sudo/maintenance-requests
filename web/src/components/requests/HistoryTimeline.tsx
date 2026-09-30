import type { HistoryEntry } from "@/lib/api/types";
import { formatDateTime } from "@/lib/format";
import { statusLabels } from "@/lib/labels";

function describe(entry: HistoryEntry): string {
  switch (entry.type) {
    case "Created":
      return "Creó la solicitud";
    case "StatusChanged":
      return entry.fromStatus && entry.toStatus
        ? `Cambió el estado de ${statusLabels[entry.fromStatus]} a ${statusLabels[entry.toStatus]}`
        : "Cambió el estado";
    case "AssigneeChanged":
      return entry.previousAssignee
        ? `Reasignó de ${entry.previousAssignee.name} a ${entry.newAssignee?.name ?? "otro responsable"}`
        : `Asignó a ${entry.newAssignee?.name ?? "un responsable"}`;
  }
}

/** Entries in the order the API returns them (oldest first). */
export function HistoryTimeline({ entries }: Readonly<{ entries: HistoryEntry[] }>) {
  return (
    <ol className="space-y-4 border-l-2 border-slate-200 pl-4">
      {entries.map((entry) => (
        <li key={entry.id} className="relative">
          <span className="absolute -left-[1.4rem] top-1.5 h-2.5 w-2.5 rounded-full bg-slate-400" aria-hidden="true" />
          <p className="text-sm text-slate-900">
            <span className="font-medium">{entry.actor.name}</span> · {describe(entry)}
          </p>
          <time dateTime={entry.occurredAt} className="text-xs text-slate-500">
            {formatDateTime(entry.occurredAt)}
          </time>
        </li>
      ))}
    </ol>
  );
}
