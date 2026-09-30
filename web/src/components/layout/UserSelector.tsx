"use client";

import { useQuery } from "@tanstack/react-query";
import { listUsers, userKeys } from "@/lib/api/users";
import { useCurrentUser } from "@/lib/current-user";

export function UserSelector() {
  const { userId, setUserId } = useCurrentUser();
  const { data: users, isPending, isError } = useQuery({ queryKey: userKeys.all, queryFn: listUsers });

  if (isError) {
    return <span className="text-sm text-red-700">No se pudieron cargar los usuarios</span>;
  }

  return (
    <label className="flex min-w-0 items-center gap-2 text-sm">
      <span className="hidden text-slate-600 sm:inline">Actuando como</span>
      <select
        className="min-w-0 max-w-44 rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm"
        value={userId ?? ""}
        disabled={isPending}
        onChange={(event) => setUserId(Number(event.target.value))}
        aria-label="Usuario actual"
      >
        <option value="" disabled>
          {isPending ? "Cargando…" : "Elige un usuario"}
        </option>
        {users?.map((user) => (
          <option key={user.id} value={user.id}>
            {user.name}
          </option>
        ))}
      </select>
    </label>
  );
}
