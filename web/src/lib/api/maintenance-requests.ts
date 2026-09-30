import { apiFetch } from "./client";
import type {
  CreateMaintenanceRequestInput,
  MaintenanceRequestDetail,
  MaintenanceRequestListItem,
  PagedResult,
  RequestListFilters,
  RequestStatus,
  Summary,
} from "./types";

const BASE_PATH = "/api/maintenance-requests";

export const PAGE_SIZE = 10;

/** Query keys in one place, so invalidation after a mutation can't miss a cache entry. */
export const requestKeys = {
  all: ["maintenance-requests"] as const,
  lists: () => [...requestKeys.all, "list"] as const,
  list: (filters: RequestListFilters) => [...requestKeys.lists(), filters] as const,
  detail: (id: number) => [...requestKeys.all, "detail", id] as const,
  summary: () => [...requestKeys.all, "summary"] as const,
};

/** Filtering, sorting and paging all happen on the server; the client only builds the query string. */
export function listRequests(filters: RequestListFilters): Promise<PagedResult<MaintenanceRequestListItem>> {
  const params = new URLSearchParams({
    page: String(filters.page),
    pageSize: String(PAGE_SIZE),
    sortDirection: filters.sortDirection,
  });
  if (filters.status) params.set("status", filters.status);
  if (filters.priority) params.set("priority", filters.priority);
  if (filters.category) params.set("category", filters.category);
  if (filters.search) params.set("search", filters.search);

  return apiFetch(`${BASE_PATH}?${params.toString()}`);
}

export function getRequest(id: number): Promise<MaintenanceRequestDetail> {
  return apiFetch(`${BASE_PATH}/${id}`);
}

export function getSummary(): Promise<Summary> {
  return apiFetch(`${BASE_PATH}/summary`);
}

export function createRequest(
  input: CreateMaintenanceRequestInput,
  userId: number,
): Promise<MaintenanceRequestDetail> {
  return apiFetch(BASE_PATH, { method: "POST", body: input, userId });
}

export function changeStatus(
  id: number,
  targetStatus: RequestStatus,
  version: number,
  userId: number,
): Promise<MaintenanceRequestDetail> {
  return apiFetch(`${BASE_PATH}/${id}/status`, {
    method: "PATCH",
    body: { targetStatus, version },
    userId,
  });
}

export function assignRequest(
  id: number,
  assigneeId: number,
  version: number,
  userId: number,
): Promise<MaintenanceRequestDetail> {
  return apiFetch(`${BASE_PATH}/${id}/assignee`, {
    method: "PATCH",
    body: { assigneeId, version },
    userId,
  });
}
