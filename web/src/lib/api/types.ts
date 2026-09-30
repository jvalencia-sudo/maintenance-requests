// Mirrors the API contracts. Enum values travel as strings, exactly as the API serializes them.

export const REQUEST_STATUSES = ["Pending", "InProgress", "OnHold", "Resolved", "Cancelled"] as const;
export type RequestStatus = (typeof REQUEST_STATUSES)[number];

export const REQUEST_CATEGORIES = ["Infrastructure", "Equipment", "Software", "Other"] as const;
export type RequestCategory = (typeof REQUEST_CATEGORIES)[number];

export const REQUEST_PRIORITIES = ["Low", "Medium", "High", "Critical"] as const;
export type RequestPriority = (typeof REQUEST_PRIORITIES)[number];

export const SORT_DIRECTIONS = ["desc", "asc"] as const;
export type SortDirection = (typeof SORT_DIRECTIONS)[number];

export type HistoryEventType = "Created" | "StatusChanged" | "AssigneeChanged";

export interface User {
  id: number;
  name: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface MaintenanceRequestListItem {
  id: number;
  title: string;
  category: RequestCategory;
  priority: RequestPriority;
  status: RequestStatus;
  assignee: User | null;
  createdAt: string;
}

export interface HistoryEntry {
  id: number;
  type: HistoryEventType;
  fromStatus: RequestStatus | null;
  toStatus: RequestStatus | null;
  previousAssignee: User | null;
  newAssignee: User | null;
  actor: User;
  occurredAt: string;
}

export interface MaintenanceRequestDetail {
  id: number;
  title: string;
  description: string;
  category: RequestCategory;
  priority: RequestPriority;
  status: RequestStatus;
  requester: User;
  assignee: User | null;
  createdAt: string;
  version: number;
  allowedTransitions: RequestStatus[];
  canAssign: boolean;
  history: HistoryEntry[];
}

export interface Summary {
  total: number;
  pending: number;
  inProgress: number;
  onHold: number;
  resolved: number;
  cancelled: number;
}

export interface RequestListFilters {
  status?: RequestStatus;
  priority?: RequestPriority;
  category?: RequestCategory;
  search?: string;
  sortDirection: SortDirection;
  page: number;
}

export interface CreateMaintenanceRequestInput {
  title: string;
  description: string;
  category: RequestCategory;
  priority: RequestPriority;
}
