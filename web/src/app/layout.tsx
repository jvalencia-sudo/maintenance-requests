import type { Metadata } from "next";
import Link from "next/link";
import "./globals.css";
import { UserSelector } from "@/components/layout/UserSelector";
import { Providers } from "./providers";

export const metadata: Metadata = {
  title: "Solicitudes de mantenimiento",
  description: "Registro y seguimiento de solicitudes de mantenimiento",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="es">
      <body className="min-h-screen">
        <Providers>
          <header className="border-b border-slate-200 bg-white">
            <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3">
              <Link href="/requests" className="shrink-0 py-2 font-semibold text-slate-900">
                Mantenimiento
              </Link>
              <UserSelector />
            </div>
          </header>
          <main className="mx-auto max-w-6xl px-4 py-6">{children}</main>
        </Providers>
      </body>
    </html>
  );
}
