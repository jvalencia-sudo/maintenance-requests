import Link from "next/link";
import { Suspense } from "react";
import { RequestListView } from "@/components/requests/RequestListView";
import { SummaryPanel } from "@/components/requests/SummaryPanel";
import { LoadingState } from "@/components/ui/LoadingState";

export default function RequestsPage() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4">
        <h1 className="text-xl font-semibold">Solicitudes</h1>
        <Link
          href="/requests/new"
          className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700"
        >
          Nueva solicitud
        </Link>
      </div>
      <SummaryPanel />
      {/* useSearchParams needs a Suspense boundary, or the static build fails. */}
      <Suspense fallback={<LoadingState />}>
        <RequestListView />
      </Suspense>
    </div>
  );
}
