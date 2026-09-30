"use client";

import { keepPreviousData, useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState } from "@/components/ui/ErrorState";
import { LoadingState } from "@/components/ui/LoadingState";
import { Pagination } from "@/components/ui/Pagination";
import { errorMessage } from "@/lib/api/client";
import { listRequests, requestKeys } from "@/lib/api/maintenance-requests";
import { useRequestFilters } from "@/lib/hooks/useRequestFilters";
import { FilterBar } from "./FilterBar";
import { RequestCard } from "./RequestCard";
import { RequestTable } from "./RequestTable";

export function RequestListView() {
  const { filters, updateFilters, setPage, clearFilters, hasActiveFilters } = useRequestFilters();

  // The filters are part of the query key: each change is a new server request.
  // keepPreviousData keeps the current page on screen while the next one loads.
  const { data, isPending, isError, error, refetch, isFetching, isPlaceholderData } = useQuery({
    queryKey: requestKeys.list(filters),
    queryFn: () => listRequests(filters),
    placeholderData: keepPreviousData,
  });

  return (
    <div className="space-y-4">
      <FilterBar
        filters={filters}
        hasActiveFilters={hasActiveFilters}
        onChange={updateFilters}
        onClear={clearFilters}
      />

      {isPending && <LoadingState label="Cargando solicitudes…" />}

      {isError && (
        <ErrorState message={errorMessage(error)} onRetry={() => void refetch()} isRetrying={isFetching} />
      )}

      {data && !isError && (
        <>
          {data.items.length === 0 ? (
            <ListEmptyState
              totalCount={data.totalCount}
              hasActiveFilters={hasActiveFilters}
              onClearFilters={clearFilters}
              onFirstPage={() => setPage(1)}
            />
          ) : (
            <div className={`space-y-4 transition-opacity ${isPlaceholderData ? "opacity-60" : ""}`}>
              <div className="hidden lg:block">
                <RequestTable requests={data.items} />
              </div>
              <ul className="grid gap-3 md:grid-cols-2 lg:hidden">
                {data.items.map((request) => (
                  <li key={request.id}>
                    <RequestCard request={request} />
                  </li>
                ))}
              </ul>
              <Pagination
                page={data.page}
                totalPages={data.totalPages}
                totalCount={data.totalCount}
                onPageChange={setPage}
                disabled={isPlaceholderData}
              />
            </div>
          )}
        </>
      )}
    </div>
  );
}

interface ListEmptyStateProps {
  totalCount: number;
  hasActiveFilters: boolean;
  onClearFilters: () => void;
  onFirstPage: () => void;
}

/** Tells apart "nothing exists yet", "nothing matches" and "this page is past the end". */
function ListEmptyState({ totalCount, hasActiveFilters, onClearFilters, onFirstPage }: Readonly<ListEmptyStateProps>) {
  const linkButton = "inline-block py-2 text-sm font-medium text-blue-700 hover:underline";

  if (totalCount === 0 && !hasActiveFilters) {
    return (
      <EmptyState
        title="Aún no hay solicitudes"
        description="Cuando alguien registre una solicitud aparecerá aquí."
        action={
          <Link href="/requests/new" className={linkButton}>
            Crear la primera solicitud
          </Link>
        }
      />
    );
  }

  if (totalCount === 0) {
    return (
      <EmptyState
        title="Ningún resultado con estos filtros"
        description="Prueba con otra búsqueda o quita algún filtro."
        action={
          <button type="button" className={linkButton} onClick={onClearFilters}>
            Limpiar filtros
          </button>
        }
      />
    );
  }

  return (
    <EmptyState
      title="Esta página no tiene resultados"
      description={`Hay ${totalCount} solicitudes, pero la página pedida está fuera del rango.`}
      action={
        <button type="button" className={linkButton} onClick={onFirstPage}>
          Ir a la primera página
        </button>
      }
    />
  );
}
