"use client";

import { useEffect, useId, useState } from "react";
import {
  REQUEST_CATEGORIES,
  REQUEST_PRIORITIES,
  REQUEST_STATUSES,
  SORT_DIRECTIONS,
  type RequestCategory,
  type RequestListFilters,
  type RequestPriority,
  type RequestStatus,
} from "@/lib/api/types";
import { categoryLabels, priorityLabels, sortDirectionLabels, statusLabels } from "@/lib/labels";

const SEARCH_DEBOUNCE_MS = 300;

interface FilterBarProps {
  filters: RequestListFilters;
  hasActiveFilters: boolean;
  onChange: (changes: Partial<Omit<RequestListFilters, "page">>) => void;
  onClear: () => void;
}

export function FilterBar({ filters, hasActiveFilters, onChange, onClear }: Readonly<FilterBarProps>) {
  const [isOpen, setIsOpen] = useState(false);
  const [searchText, setSearchText] = useState(filters.search ?? "");
  const panelId = useId();

  // Debounce: the URL (and so the request) only changes once the user stops typing.
  useEffect(() => {
    const search = searchText.trim();
    if (search === (filters.search ?? "")) {
      return;
    }
    const timer = setTimeout(() => onChange({ search: search || undefined }), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [searchText, filters.search, onChange]);

  const clear = () => {
    setSearchText("");
    onClear();
  };

  const selectClass = "w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm";

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4">
      <div className="flex items-center gap-2">
        <input
          type="search"
          value={searchText}
          onChange={(event) => setSearchText(event.target.value)}
          placeholder="Buscar por título…"
          aria-label="Buscar por título"
          className="min-w-0 flex-1 rounded-md border border-slate-300 px-3 py-2 text-sm"
        />
        <button
          type="button"
          onClick={() => setIsOpen((open) => !open)}
          aria-expanded={isOpen}
          aria-controls={panelId}
          className="shrink-0 rounded-md border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 md:hidden"
        >
          Filtros{hasActiveFilters ? " •" : ""}
        </button>
      </div>

      <div id={panelId} className={`${isOpen ? "grid" : "hidden"} mt-3 gap-3 sm:grid-cols-2 md:grid md:grid-cols-4`}>
        <FilterSelect
          label="Estado"
          value={filters.status}
          options={REQUEST_STATUSES}
          labels={statusLabels}
          onChange={(status: RequestStatus | undefined) => onChange({ status })}
          className={selectClass}
        />
        <FilterSelect
          label="Prioridad"
          value={filters.priority}
          options={REQUEST_PRIORITIES}
          labels={priorityLabels}
          onChange={(priority: RequestPriority | undefined) => onChange({ priority })}
          className={selectClass}
        />
        <FilterSelect
          label="Categoría"
          value={filters.category}
          options={REQUEST_CATEGORIES}
          labels={categoryLabels}
          onChange={(category: RequestCategory | undefined) => onChange({ category })}
          className={selectClass}
        />
        <label className="text-sm">
          <span className="mb-1 block text-slate-600">Orden</span>
          <select
            value={filters.sortDirection}
            onChange={(event) =>
              onChange({ sortDirection: SORT_DIRECTIONS.find((d) => d === event.target.value) ?? "desc" })
            }
            className={selectClass}
          >
            {SORT_DIRECTIONS.map((direction) => (
              <option key={direction} value={direction}>
                {sortDirectionLabels[direction]}
              </option>
            ))}
          </select>
        </label>
      </div>

      {hasActiveFilters && (
        <button type="button" onClick={clear} className="mt-3 text-sm font-medium text-blue-700 hover:underline">
          Limpiar filtros
        </button>
      )}
    </section>
  );
}

interface FilterSelectProps<T extends string> {
  label: string;
  value: T | undefined;
  options: readonly T[];
  labels: Record<T, string>;
  onChange: (value: T | undefined) => void;
  className: string;
}

function FilterSelect<T extends string>({ label, value, options, labels, onChange, className }: Readonly<FilterSelectProps<T>>) {
  return (
    <label className="text-sm">
      <span className="mb-1 block text-slate-600">{label}</span>
      <select
        value={value ?? ""}
        onChange={(event) => onChange(options.find((option) => option === event.target.value))}
        className={className}
      >
        <option value="">Todas</option>
        {options.map((option) => (
          <option key={option} value={option}>
            {labels[option]}
          </option>
        ))}
      </select>
    </label>
  );
}
