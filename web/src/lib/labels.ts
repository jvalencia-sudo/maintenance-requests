import type { RequestCategory, RequestPriority, RequestStatus, SortDirection } from "./api/types";

// Presentation only: the API speaks in enum values, the UI shows them in Spanish.
// Translating a value is not a business rule, so this is not duplicated logic.

export const statusLabels: Record<RequestStatus, string> = {
  Pending: "Pendiente",
  InProgress: "En progreso",
  OnHold: "En espera",
  Resolved: "Resuelta",
  Cancelled: "Cancelada",
};

export const priorityLabels: Record<RequestPriority, string> = {
  Low: "Baja",
  Medium: "Media",
  High: "Alta",
  Critical: "Crítica",
};

export const categoryLabels: Record<RequestCategory, string> = {
  Infrastructure: "Infraestructura",
  Equipment: "Equipos",
  Software: "Software",
  Other: "Otro",
};

export const sortDirectionLabels: Record<SortDirection, string> = {
  desc: "Más recientes primero",
  asc: "Más antiguas primero",
};
