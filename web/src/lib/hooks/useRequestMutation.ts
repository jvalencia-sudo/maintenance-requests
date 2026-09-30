"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ApiError, errorMessage } from "@/lib/api/client";
import { requestKeys } from "@/lib/api/maintenance-requests";
import type { MaintenanceRequestDetail } from "@/lib/api/types";

const CONFLICT_STATUS = 409;

/** Receives the message to show after a change, or null once a change succeeds. */
export type FeedbackHandler = (message: string | null) => void;

/**
 * Shared behavior for every change made from the detail page (status, assignee):
 * the response becomes the new detail, and the list and summary are refreshed.
 * Feedback is reported upwards so it survives the actions being re-rendered or hidden
 * (e.g. a conflict reveals the request was closed, and the form disappears).
 */
export function useRequestMutation<TVariables>(
  requestId: number,
  mutationFn: (variables: TVariables) => Promise<MaintenanceRequestDetail>,
  onFeedback: FeedbackHandler,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn,
    onSuccess: (updated) => {
      onFeedback(null);
      queryClient.setQueryData(requestKeys.detail(requestId), updated);
      void queryClient.invalidateQueries({ queryKey: requestKeys.lists() });
      void queryClient.invalidateQueries({ queryKey: requestKeys.summary() });
    },
    onError: (error) => {
      onFeedback(mutationErrorMessage(error));
      // A 409 means the screen no longer matches the server: reload what is really there.
      if (error instanceof ApiError && error.status === CONFLICT_STATUS) {
        void queryClient.invalidateQueries({ queryKey: requestKeys.detail(requestId) });
      }
    },
  });
}

function mutationErrorMessage(error: unknown): string {
  if (error instanceof ApiError && error.code === "concurrency_conflict") {
    return "Otro usuario modificó esta solicitud. Se cargaron los datos más recientes; revísalos e intenta de nuevo.";
  }
  return errorMessage(error);
}
