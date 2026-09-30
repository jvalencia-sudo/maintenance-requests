"use client";

import { useQuery } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { assignRequest } from "@/lib/api/maintenance-requests";
import type { MaintenanceRequestDetail } from "@/lib/api/types";
import { listUsers, userKeys } from "@/lib/api/users";
import { useCurrentUser } from "@/lib/current-user";
import { useRequestMutation, type FeedbackHandler } from "@/lib/hooks/useRequestMutation";

interface AssignVariables {
  assigneeId: number;
  userId: number;
}

interface AssignFormProps {
  request: MaintenanceRequestDetail;
  onFeedback: FeedbackHandler;
}

/** Rendered only when the API reports canAssign. */
export function AssignForm({ request, onFeedback }: Readonly<AssignFormProps>) {
  const { userId } = useCurrentUser();
  const { data: users } = useQuery({ queryKey: userKeys.all, queryFn: listUsers, staleTime: Infinity });
  const [assigneeId, setAssigneeId] = useState<number | null>(null);
  const mutation = useRequestMutation(
    request.id,
    ({ assigneeId, userId }: AssignVariables) => assignRequest(request.id, assigneeId, request.version, userId),
    onFeedback,
  );

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (assigneeId !== null && userId !== null) {
      mutation.mutate({ assigneeId, userId }, { onSuccess: () => setAssigneeId(null) });
    }
  };

  return (
    <form onSubmit={submit} className="space-y-3">
      {/* Stacked in the narrow side column on lg; side by side where there is room. */}
      <div className="flex flex-col gap-2 sm:flex-row lg:flex-col xl:flex-row">
        <select
          value={assigneeId ?? ""}
          onChange={(event) => setAssigneeId(event.target.value ? Number(event.target.value) : null)}
          aria-label="Nuevo responsable"
          className="min-w-0 flex-1 rounded-md border border-slate-300 bg-white px-3 py-2 text-base sm:text-sm"
        >
          <option value="">Selecciona un responsable</option>
          {users?.map((user) => (
            <option key={user.id} value={user.id}>
              {user.name}
            </option>
          ))}
        </select>
        <button
          type="submit"
          disabled={assigneeId === null || userId === null || mutation.isPending}
          className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {mutation.isPending ? "Guardando…" : request.assignee ? "Reasignar" : "Asignar"}
        </button>
      </div>

      {userId === null && <p className="text-sm text-slate-600">Elige un usuario en el encabezado para asignar.</p>}
    </form>
  );
}
