interface ErrorStateProps {
  title?: string;
  message: string;
  onRetry?: () => void;
  isRetrying?: boolean;
}

export function ErrorState({
  title = "No se pudo cargar la información",
  message,
  onRetry,
  isRetrying = false,
}: Readonly<ErrorStateProps>) {
  return (
    <div role="alert" className="rounded-lg border border-red-200 bg-red-50 px-6 py-8 text-center">
      <p className="font-medium text-red-900">{title}</p>
      <p className="mt-1 text-sm text-red-800">{message}</p>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          disabled={isRetrying}
          className="mt-4 rounded-md bg-red-700 px-4 py-2 text-sm font-medium text-white hover:bg-red-800 disabled:opacity-60"
        >
          {isRetrying ? "Reintentando…" : "Reintentar"}
        </button>
      )}
    </div>
  );
}
