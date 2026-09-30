export function LoadingState({ label = "Cargando…" }: Readonly<{ label?: string }>) {
  return (
    <div role="status" className="flex items-center justify-center gap-3 py-12 text-sm text-slate-600">
      <span
        className="h-5 w-5 animate-spin rounded-full border-2 border-slate-300 border-t-slate-700"
        aria-hidden="true"
      />
      {label}
    </div>
  );
}
