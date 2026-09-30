import type { RequestPriority } from "@/lib/api/types";
import { priorityLabels } from "@/lib/labels";

const styles: Record<RequestPriority, string> = {
  Low: "text-slate-600",
  Medium: "text-sky-700",
  High: "text-orange-700",
  Critical: "text-red-700 font-semibold",
};

const dots: Record<RequestPriority, string> = {
  Low: "bg-slate-400",
  Medium: "bg-sky-500",
  High: "bg-orange-500",
  Critical: "bg-red-600",
};

export function PriorityBadge({ priority }: Readonly<{ priority: RequestPriority }>) {
  return (
    <span className={`inline-flex items-center gap-1.5 whitespace-nowrap text-xs ${styles[priority]}`}>
      <span className={`h-2 w-2 rounded-full ${dots[priority]}`} aria-hidden="true" />
      {priorityLabels[priority]}
    </span>
  );
}
