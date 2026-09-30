"use client";

import { useQuery } from "@tanstack/react-query";
import { errorMessage } from "@/lib/api/client";
import { getSummary, requestKeys } from "@/lib/api/maintenance-requests";
import type { Summary } from "@/lib/api/types";
import { statusLabels } from "@/lib/labels";

const figures: { key: keyof Summary; label: string; accent: string }[] = [
  { key: "total", label: "Total", accent: "border-slate-300" },
  { key: "pending", label: statusLabels.Pending, accent: "border-amber-400" },
  { key: "inProgress", label: statusLabels.InProgress, accent: "border-blue-400" },
  { key: "onHold", label: statusLabels.OnHold, accent: "border-violet-400" },
  { key: "resolved", label: statusLabels.Resolved, accent: "border-emerald-400" },
  { key: "cancelled", label: statusLabels.Cancelled, accent: "border-slate-400" },
];

/** Global totals by status. They do not follow the list filters. */
export function SummaryPanel() {
  const { data, isPending, isError, error, refetch } = useQuery({
    queryKey: requestKeys.summary(),
    queryFn: getSummary,
  });

  return (
    <section aria-labelledby="summary-heading">
      <h2 id="summary-heading" className="mb-2 text-sm font-medium text-slate-600">
        Resumen general
      </h2>

      {isError ? (
        <p className="text-sm text-red-700">
          {errorMessage(error)}{" "}
          <button type="button" onClick={() => void refetch()} className="font-medium underline">
            Reintentar
          </button>
        </p>
      ) : (
        <dl className="grid grid-cols-3 gap-2 sm:gap-3 lg:grid-cols-6">
          {figures.map(({ key, label, accent }) => (
            <div key={key} className={`rounded-lg border-l-4 bg-white px-2.5 py-2 shadow-sm sm:px-4 sm:py-3 ${accent}`}>
              <dt className="truncate text-xs text-slate-600">{label}</dt>
              <dd className="mt-1 text-xl font-semibold sm:text-2xl text-slate-900">
                {isPending ? <span className="inline-block h-7 w-8 animate-pulse rounded bg-slate-200" /> : data[key]}
              </dd>
            </div>
          ))}
        </dl>
      )}
    </section>
  );
}
