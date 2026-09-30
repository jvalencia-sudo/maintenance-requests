"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";
import { ApiError } from "@/lib/api/client";
import { CurrentUserProvider } from "@/lib/current-user";

export function Providers({ children }: Readonly<{ children: ReactNode }>) {
  // One client per browser session; useState keeps it stable across re-renders.
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            // staleTime 0 (the default): cached data shows instantly, but every visit to a
            // list, detail or summary is revalidated against the server.
            // A 4xx (not found, invalid input) will not fix itself: only retry network and server errors.
            retry: (failureCount, error) =>
              failureCount < 1 && !(error instanceof ApiError && error.status >= 400 && error.status < 500),
            refetchOnWindowFocus: false,
          },
        },
      }),
  );

  return (
    <QueryClientProvider client={queryClient}>
      <CurrentUserProvider>{children}</CurrentUserProvider>
    </QueryClientProvider>
  );
}
