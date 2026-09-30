"use client";

import { createContext, useCallback, useContext, useMemo, useSyncExternalStore, type ReactNode } from "react";

const STORAGE_KEY = "maintenance-requests.current-user-id";

// localStorage as an external store: every component reads the same value,
// and the server render (no localStorage) consistently sees "no user selected".
const listeners = new Set<() => void>();

function subscribe(listener: () => void) {
  listeners.add(listener);
  window.addEventListener("storage", listener);
  return () => {
    listeners.delete(listener);
    window.removeEventListener("storage", listener);
  };
}

function readStoredUserId(): number | null {
  const stored = window.localStorage.getItem(STORAGE_KEY);
  const id = stored === null ? NaN : Number(stored);
  return Number.isInteger(id) && id > 0 ? id : null;
}

interface CurrentUserContextValue {
  /** The user acting in the app, sent as X-User-Id on mutations; null until one is chosen. */
  userId: number | null;
  setUserId: (id: number) => void;
}

const CurrentUserContext = createContext<CurrentUserContextValue | null>(null);

export function CurrentUserProvider({ children }: Readonly<{ children: ReactNode }>) {
  const userId = useSyncExternalStore(subscribe, readStoredUserId, () => null);

  const setUserId = useCallback((id: number) => {
    window.localStorage.setItem(STORAGE_KEY, String(id));
    listeners.forEach((listener) => listener());
  }, []);

  const value = useMemo(() => ({ userId, setUserId }), [userId, setUserId]);

  return <CurrentUserContext.Provider value={value}>{children}</CurrentUserContext.Provider>;
}

export function useCurrentUser(): CurrentUserContextValue {
  const context = useContext(CurrentUserContext);
  if (!context) {
    throw new Error("useCurrentUser must be used inside CurrentUserProvider.");
  }
  return context;
}
