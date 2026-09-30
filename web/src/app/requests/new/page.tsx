import Link from "next/link";
import { CreateRequestForm } from "@/components/requests/CreateRequestForm";

export default function NewRequestPage() {
  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <Link href="/requests" className="inline-block py-2 text-sm font-medium text-blue-700 hover:underline">
        ← Volver al listado
      </Link>
      <h1 className="text-xl font-semibold">Nueva solicitud</h1>
      <CreateRequestForm />
    </div>
  );
}
