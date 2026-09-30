"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";
import { CurrentUserProvider } from "@/lib/current-user";

export function Providers({ children }: Readonly<{ children: ReactNode }>) {
  // One client per browser session; useState keeps it stable across re-renders.
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30_000,
            retry: 1,
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
