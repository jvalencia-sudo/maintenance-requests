import { apiFetch } from "./client";
import type { User } from "./types";

export const userKeys = {
  all: ["users"] as const,
};

export function listUsers(): Promise<User[]> {
  return apiFetch("/api/users");
}
