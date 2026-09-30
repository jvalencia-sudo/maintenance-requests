"use client";

import { changeStatus } from "@/lib/api/maintenance-requests";
import type { MaintenanceRequestDetail, RequestStatus } from "@/lib/api/types";
import { useCurrentUser } from "@/lib/current-user";
import { useRequestMutation, type FeedbackHandler } from "@/lib/hooks/useRequestMutation";
import { transitionLabels } from "@/lib/labels";

interface ChangeStatusVariables {
  targetStatus: RequestStatus;
  userId: number;
}

interface StatusActionsProps {
  request: MaintenanceRequestDetail;
  onFeedback: FeedbackHandler;
}

/**
 * One button per status the API says is reachable (allowedTransitions).
 * The UI never decides which transitions are valid.
 */
export function StatusActions({ request, onFeedback }: Readonly<StatusActionsProps>) {
  const { userId } = useCurrentUser();
  const mutation = useRequestMutation(
    request.id,
    ({ targetStatus, userId }: ChangeStatusVariables) => changeStatus(request.id, targetStatus, request.version, userId),
    onFeedback,
  );

  if (request.allowedTransitions.length === 0) {
    return <p className="text-sm text-slate-600">La solicitud está cerrada; su estado ya no puede cambiar.</p>;
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap gap-2">
        {request.allowedTransitions.map((targetStatus) => {
          const isSubmitting = mutation.isPending && mutation.variables?.targetStatus === targetStatus;
          return (
            <button
              key={targetStatus}
              type="button"
              disabled={mutation.isPending || userId === null}
              onClick={() => userId !== null && mutation.mutate({ targetStatus, userId })}
              className={
                targetStatus === "Cancelled"
                  ? "rounded-md border border-red-300 bg-white px-3 py-2 text-sm font-medium text-red-700 hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-50"
                  : "rounded-md bg-slate-900 px-3 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
              }
            >
              {isSubmitting ? "Guardando…" : transitionLabels[targetStatus]}
            </button>
          );
        })}
      </div>

      {userId === null && (
        <p className="text-sm text-slate-600">Elige un usuario en el encabezado para cambiar el estado.</p>
      )}
    </div>
  );
}
