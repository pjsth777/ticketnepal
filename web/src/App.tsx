import { useEffect, useState } from "react";

const API_URL = import.meta.env.VITE_API_URL as string;

type Status = "checking" | "waking" | "online" | "offline";

const messages: Record<Status, string> = {
  checking: "Connecting to the server...",
  waking: "Waking up the server... free-tier hosting sleeps when idle, this can take up to a minute.",
  online: "API is online.",
  offline: "Could not reach the API.",
};

export default function App() {
  const [status, setStatus] = useState<Status>("checking");
  const [total, setTotal] = useState<number | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    const wakingTimer = setTimeout(
      () => setStatus((s) => (s === "checking" ? "waking" : s)),
      3000,
    );

    (async () => {
      try {
        const res = await fetch(`${API_URL}/api/v1/events`, { signal: controller.signal });
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        setTotal(data.total);
        setStatus("online");
      } catch {
        if (!controller.signal.aborted) setStatus("offline");
      } finally {
        clearTimeout(wakingTimer);
      }
    })();

    return () => {
      controller.abort();
      clearTimeout(wakingTimer);
    }
  }, []);

  return (
    <main className="min-h-screen flex flex-col items-center justify-center gap-3 bg-slate-950 text-slate-100 p-6">
      <h1 className="text-4xl font-bold tracking-tight">TicketNepal</h1>
      <p className="text-slate-400">Event ticketing, built in the open.</p>
      <p
        className={
          status === "online"
            ? "text-emerald-400"
            : status === "offline"
              ? "text-rose-400"
              : "text-amber-400"
        }
      >
        {messages[status]}
        {status === "online" && total !== null && ` ${total} events listed.`}
      </p>
    </main>
  )
} 