import { notFound } from "next/navigation";
import { RequestDetailView } from "@/components/requests/RequestDetailView";

export default async function RequestDetailPage({ params }: PageProps<"/requests/[id]">) {
  const { id } = await params;
  const requestId = Number(id);
  if (!Number.isInteger(requestId) || requestId <= 0) {
    notFound();
  }

  return <RequestDetailView id={requestId} />;
}
