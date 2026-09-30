interface PaginationProps {
  page: number;
  totalPages: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  disabled?: boolean;
}

export function Pagination({ page, totalPages, totalCount, onPageChange, disabled = false }: Readonly<PaginationProps>) {
  const buttonClass =
    "rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50";

  return (
    <nav aria-label="Paginación" className="flex flex-wrap items-center justify-between gap-3">
      <p className="text-sm text-slate-600">
        Página {page} de {Math.max(totalPages, 1)} · {totalCount} {totalCount === 1 ? "solicitud" : "solicitudes"}
      </p>
      <div className="flex gap-2">
        <button
          type="button"
          className={buttonClass}
          disabled={disabled || page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          Anterior
        </button>
        <button
          type="button"
          className={buttonClass}
          disabled={disabled || page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          Siguiente
        </button>
      </div>
    </nav>
  );
}
