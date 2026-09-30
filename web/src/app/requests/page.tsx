import { Suspense } from "react";
import { RequestListView } from "@/components/requests/RequestListView";
import { SummaryPanel } from "@/components/requests/SummaryPanel";
import { LoadingState } from "@/components/ui/LoadingState";

export default function RequestsPage() {
  return (
    <div className="space-y-6">
      <h1 className="text-xl font-semibold">Solicitudes</h1>
      <SummaryPanel />
      {/* useSearchParams needs a Suspense boundary, or the static build fails. */}
      <Suspense fallback={<LoadingState />}>
        <RequestListView />
      </Suspense>
    </div>
  );
}
