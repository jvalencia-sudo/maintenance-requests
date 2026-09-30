"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useCallback, useMemo } from "react";
import {
  REQUEST_CATEGORIES,
  REQUEST_PRIORITIES,
  REQUEST_STATUSES,
  SORT_DIRECTIONS,
  type RequestListFilters,
} from "@/lib/api/types";

type FilterChanges = Partial<Omit<RequestListFilters, "page">>;

/**
 * The URL is the single source of truth for the list filters: they survive a reload,
 * can be shared as a link, and every change produces a new server request.
 */
export function useRequestFilters() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

  const filters = useMemo(() => parseFilters(searchParams), [searchParams]);

  const navigate = useCallback(
    (next: RequestListFilters) => {
      const query = toSearchParams(next).toString();
      router.replace(query ? `${pathname}?${query}` : pathname, { scroll: false });
    },
    [router, pathname],
  );

  /** Any filter change goes back to the first page: the old page may not exist in the new result. */
  const updateFilters = useCallback(
    (changes: FilterChanges) => navigate({ ...filters, ...changes, page: 1 }),
    [navigate, filters],
  );

  const setPage = useCallback((page: number) => navigate({ ...filters, page }), [navigate, filters]);

  const clearFilters = useCallback(
    () => navigate({ sortDirection: filters.sortDirection, page: 1 }),
    [navigate, filters.sortDirection],
  );

  const hasActiveFilters = Boolean(filters.status || filters.priority || filters.category || filters.search);

  return { filters, updateFilters, setPage, clearFilters, hasActiveFilters };
}

function parseFilters(params: URLSearchParams): RequestListFilters {
  const page = Number(params.get("page"));

  return {
    status: oneOf(params.get("status"), REQUEST_STATUSES),
    priority: oneOf(params.get("priority"), REQUEST_PRIORITIES),
    category: oneOf(params.get("category"), REQUEST_CATEGORIES),
    search: params.get("search")?.trim() || undefined,
    sortDirection: oneOf(params.get("sortDirection"), SORT_DIRECTIONS) ?? "desc",
    page: Number.isInteger(page) && page > 0 ? page : 1,
  };
}

/** Defaults are left out so a clean list has a clean URL. */
function toSearchParams(filters: RequestListFilters): URLSearchParams {
  const params = new URLSearchParams();
  if (filters.status) params.set("status", filters.status);
  if (filters.priority) params.set("priority", filters.priority);
  if (filters.category) params.set("category", filters.category);
  if (filters.search) params.set("search", filters.search);
  if (filters.sortDirection !== "desc") params.set("sortDirection", filters.sortDirection);
  if (filters.page > 1) params.set("page", String(filters.page));
  return params;
}

/** Ignores values that are not in the catalog, e.g. a hand-edited URL. */
function oneOf<T extends string>(value: string | null, allowed: readonly T[]): T | undefined {
  return allowed.find((candidate) => candidate === value);
}
