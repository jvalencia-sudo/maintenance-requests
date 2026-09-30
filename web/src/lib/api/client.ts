/** Field name (camelCase, as the API reports it) to its error messages. */
export type FieldErrors = Record<string, string[]>;

/** A non-2xx response, parsed from the API's ProblemDetails body. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly title: string,
    readonly detail: string | undefined,
    readonly code: string | undefined,
    readonly fieldErrors: FieldErrors,
  ) {
    super(detail ?? title);
    this.name = "ApiError";
  }
}

interface ApiRequestInit {
  method?: "GET" | "POST" | "PATCH";
  body?: unknown;
  /** Sent as X-User-Id; the API requires it on mutations. */
  userId?: number | null;
}

const NETWORK_ERROR_STATUS = 0;

function apiBaseUrl(): string {
  const url = process.env.NEXT_PUBLIC_API_URL;
  if (!url) {
    throw new Error("NEXT_PUBLIC_API_URL is not configured.");
  }
  return url.replace(/\/$/, "");
}

/** The only place in the app that calls fetch. */
export async function apiFetch<T>(path: string, init: ApiRequestInit = {}): Promise<T> {
  const headers = new Headers({ Accept: "application/json" });
  if (init.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }
  if (init.userId != null) {
    headers.set("X-User-Id", String(init.userId));
  }

  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl()}${path}`, {
      method: init.method ?? "GET",
      headers,
      body: init.body === undefined ? undefined : JSON.stringify(init.body),
    });
  } catch {
    throw new ApiError(
      NETWORK_ERROR_STATUS,
      "No se pudo conectar con el servidor.",
      "Revisa tu conexión o intenta de nuevo en unos segundos.",
      "network_error",
      {},
    );
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  return (await response.json()) as T;
}

/** A user-facing message for any error thrown while talking to the API. */
export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.detail ?? error.title;
  }
  return "Ocurrió un error inesperado.";
}

async function toApiError(response: Response): Promise<ApiError> {
  const body: unknown = await response.json().catch(() => null);
  const problem = isRecord(body) ? body : {};

  return new ApiError(
    response.status,
    asString(problem.title) ?? "La solicitud no se pudo completar.",
    asString(problem.detail),
    asString(problem.code),
    parseFieldErrors(problem.errors),
  );
}

function parseFieldErrors(value: unknown): FieldErrors {
  if (!isRecord(value)) {
    return {};
  }

  const errors: FieldErrors = {};
  for (const [key, messages] of Object.entries(value)) {
    if (Array.isArray(messages)) {
      // Body deserialization errors come as JSON paths ("$.priority"); keep only the field name.
      errors[key.replace(/^\$\.?/, "")] = messages.filter((m): m is string => typeof m === "string");
    }
  }
  return errors;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function asString(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}
