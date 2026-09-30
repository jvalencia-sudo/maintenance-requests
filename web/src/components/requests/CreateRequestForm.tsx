"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useId, useState, type FormEvent, type ReactNode } from "react";
import { ApiError, errorMessage } from "@/lib/api/client";
import { createRequest, requestKeys } from "@/lib/api/maintenance-requests";
import {
  REQUEST_CATEGORIES,
  REQUEST_PRIORITIES,
  type CreateMaintenanceRequestInput,
  type RequestCategory,
  type RequestPriority,
} from "@/lib/api/types";
import { useCurrentUser } from "@/lib/current-user";
import { categoryLabels, priorityLabels } from "@/lib/labels";
import {
  DESCRIPTION_MAX_LENGTH,
  TITLE_MAX_LENGTH,
  textLength,
  validateCreateRequest,
  type CreateRequestFormErrors,
  type CreateRequestFormValues,
} from "@/lib/validation";

const BAD_REQUEST_STATUS = 400;
const FORM_FIELDS = ["title", "description", "category", "priority"] as const;

const initialValues: CreateRequestFormValues = { title: "", description: "", category: "", priority: "" };

export function CreateRequestForm() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { userId } = useCurrentUser();
  const [values, setValues] = useState(initialValues);
  const [errors, setErrors] = useState<CreateRequestFormErrors>({});
  const [formError, setFormError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: ({ input, userId }: { input: CreateMaintenanceRequestInput; userId: number }) =>
      createRequest(input, userId),
    onSuccess: (created) => {
      queryClient.setQueryData(requestKeys.detail(created.id), created);
      void queryClient.invalidateQueries({ queryKey: requestKeys.lists() });
      void queryClient.invalidateQueries({ queryKey: requestKeys.summary() });
      router.push(`/requests/${created.id}`);
    },
    onError: (error) => {
      // Field errors from the server's 400 go under their field; anything else is a form-level message.
      if (error instanceof ApiError && error.status === BAD_REQUEST_STATUS) {
        const serverErrors: CreateRequestFormErrors = {};
        for (const field of FORM_FIELDS) {
          const message = error.fieldErrors[field]?.[0];
          if (message) serverErrors[field] = message;
        }
        setErrors(serverErrors);
        setFormError(Object.keys(serverErrors).length ? null : errorMessage(error));
        return;
      }
      setFormError(errorMessage(error));
    },
  });

  const update = <K extends keyof CreateRequestFormValues>(field: K, value: CreateRequestFormValues[K]) => {
    setValues((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
  };

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);
    const clientErrors = validateCreateRequest(values);
    setErrors(clientErrors);
    if (Object.keys(clientErrors).length > 0 || userId === null || !values.category || !values.priority) {
      return;
    }
    mutation.mutate({
      input: {
        title: values.title,
        description: values.description,
        category: values.category,
        priority: values.priority,
      },
      userId,
    });
  };

  // 16px on phones: iOS Safari zooms into any form control with a smaller font.
  const inputClass = (hasError: boolean) =>
    `w-full rounded-md border bg-white px-3 py-2 text-base sm:text-sm ${hasError ? "border-red-500" : "border-slate-300"}`;

  return (
    <form onSubmit={submit} noValidate className="space-y-5 rounded-lg border border-slate-200 bg-white p-4 sm:p-6">
      {formError && (
        <p role="alert" className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
          {formError}
        </p>
      )}

      <Field label="Título" error={errors.title} hint={`${textLength(values.title)}/${TITLE_MAX_LENGTH}`}>
        {(fieldProps) => (
          <input
            {...fieldProps}
            type="text"
            value={values.title}
            onChange={(event) => update("title", event.target.value)}
            className={inputClass(Boolean(errors.title))}
          />
        )}
      </Field>

      <Field
        label="Descripción"
        error={errors.description}
        hint={`${textLength(values.description)}/${DESCRIPTION_MAX_LENGTH}`}
      >
        {(fieldProps) => (
          <textarea
            {...fieldProps}
            rows={5}
            value={values.description}
            onChange={(event) => update("description", event.target.value)}
            className={inputClass(Boolean(errors.description))}
          />
        )}
      </Field>

      <div className="grid gap-5 sm:grid-cols-2">
        <Field label="Categoría" error={errors.category}>
          {(fieldProps) => (
            <select
              {...fieldProps}
              value={values.category}
              onChange={(event) => update("category", REQUEST_CATEGORIES.find((c) => c === event.target.value) ?? "")}
              className={inputClass(Boolean(errors.category))}
            >
              <option value="">Selecciona una categoría</option>
              {REQUEST_CATEGORIES.map((category: RequestCategory) => (
                <option key={category} value={category}>
                  {categoryLabels[category]}
                </option>
              ))}
            </select>
          )}
        </Field>

        <Field label="Prioridad" error={errors.priority}>
          {(fieldProps) => (
            <select
              {...fieldProps}
              value={values.priority}
              onChange={(event) => update("priority", REQUEST_PRIORITIES.find((p) => p === event.target.value) ?? "")}
              className={inputClass(Boolean(errors.priority))}
            >
              <option value="">Selecciona una prioridad</option>
              {REQUEST_PRIORITIES.map((priority: RequestPriority) => (
                <option key={priority} value={priority}>
                  {priorityLabels[priority]}
                </option>
              ))}
            </select>
          )}
        </Field>
      </div>

      <div className="flex flex-col-reverse items-stretch gap-3 sm:flex-row sm:items-center sm:justify-end">
        {userId === null && (
          <p className="text-sm text-slate-600">Elige un usuario en el encabezado para registrar la solicitud.</p>
        )}
        <button
          type="submit"
          disabled={mutation.isPending || userId === null}
          className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {mutation.isPending ? "Creando…" : "Crear solicitud"}
        </button>
      </div>
    </form>
  );
}

interface FieldControlProps {
  id: string;
  "aria-invalid": boolean;
  "aria-describedby"?: string;
}

interface FieldProps {
  label: string;
  error?: string;
  hint?: string;
  children: (props: FieldControlProps) => ReactNode;
}

/** Label, control and error message wired together for screen readers. */
function Field({ label, error, hint, children }: Readonly<FieldProps>) {
  const id = useId();
  const errorId = `${id}-error`;

  return (
    <div>
      <div className="mb-1 flex items-baseline justify-between gap-2">
        <label htmlFor={id} className="text-sm font-medium text-slate-700">
          {label}
        </label>
        {hint && <span className="text-xs text-slate-500">{hint}</span>}
      </div>
      {children({ id, "aria-invalid": Boolean(error), "aria-describedby": error ? errorId : undefined })}
      {error && (
        <p id={errorId} className="mt-1 text-sm text-red-700">
          {error}
        </p>
      )}
    </div>
  );
}
