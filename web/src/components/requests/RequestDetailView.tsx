"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { useId, useState, type ReactNode } from "react";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState } from "@/components/ui/ErrorState";
import { LoadingState } from "@/components/ui/LoadingState";
import { PriorityBadge } from "@/components/ui/PriorityBadge";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { ApiError, errorMessage } from "@/lib/api/client";
import { getRequest, requestKeys } from "@/lib/api/maintenance-requests";
import { formatDateTime } from "@/lib/format";
import { categoryLabels } from "@/lib/labels";
import { AssignForm } from "./AssignForm";
import { HistoryTimeline } from "./HistoryTimeline";
import { StatusActions } from "./StatusActions";

const NOT_FOUND_STATUS = 404;

export function RequestDetailView({ id }: Readonly<{ id: number }>) {
  // Result of the last change (status or assignee), kept here so it outlives the action that produced it.
  const [feedback, setFeedback] = useState<string | null>(null);
  const { data: request, isPending, isError, error, refetch, isFetching } = useQuery({
    queryKey: requestKeys.detail(id),
    queryFn: () => getRequest(id),
  });

  const backLink = (
    <Link href="/requests" className="text-sm font-medium text-blue-700 hover:underline">
      ← Volver al listado
    </Link>
  );

  if (isPending) {
    return <LoadingState label="Cargando solicitud…" />;
  }

  if (isError) {
    return error instanceof ApiError && error.status === NOT_FOUND_STATUS ? (
      <EmptyState title="Solicitud no encontrada" description={`No existe una solicitud con id ${id}.`} action={backLink} />
    ) : (
      <ErrorState message={errorMessage(error)} onRetry={() => void refetch()} isRetrying={isFetching} />
    );
  }

  return (
    <div className="space-y-6">
      {backLink}

      <header className="space-y-2">
        <p className="text-sm text-slate-500">Solicitud #{request.id}</p>
        <h1 className="break-words text-2xl font-semibold text-slate-900">{request.title}</h1>
        <div className="flex flex-wrap items-center gap-3">
          <StatusBadge status={request.status} />
          <PriorityBadge priority={request.priority} />
        </div>
      </header>

      {feedback && (
        <div role="alert" className="flex items-start justify-between gap-3 rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          <p>{feedback}</p>
          <button type="button" onClick={() => setFeedback(null)} className="shrink-0 font-medium underline">
            Cerrar
          </button>
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <Card title="Descripción">
            <p className="whitespace-pre-wrap break-words text-sm text-slate-800">{request.description}</p>
          </Card>

          <Card title="Historial">
            <HistoryTimeline entries={request.history} />
          </Card>
        </div>

        <div className="space-y-6">
          <Card title="Datos">
            <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
              <dt className="text-slate-500">Categoría</dt>
              <dd>{categoryLabels[request.category]}</dd>
              <dt className="text-slate-500">Solicitante</dt>
              <dd className="break-words">{request.requester.name}</dd>
              <dt className="text-slate-500">Responsable</dt>
              <dd className="break-words">{request.assignee?.name ?? "Sin asignar"}</dd>
              <dt className="text-slate-500">Creada</dt>
              <dd>{formatDateTime(request.createdAt)}</dd>
            </dl>
          </Card>

          <Card title="Cambiar estado">
            <StatusActions request={request} onFeedback={setFeedback} />
          </Card>

          {request.canAssign && (
            <Card title="Responsable">
              <AssignForm request={request} onFeedback={setFeedback} />
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}

function Card({ title, children }: Readonly<{ title: string; children: ReactNode }>) {
  const headingId = useId();
  return (
    <section aria-labelledby={headingId} className="rounded-lg border border-slate-200 bg-white p-4">
      <h2 id={headingId} className="mb-3 text-sm font-semibold text-slate-700">
        {title}
      </h2>
      {children}
    </section>
  );
}
