import { parseCards } from "./guard";
import type { CardsResult } from "./types";

type FetchFn = typeof fetch;

// Next answers with a bare 5xx (not ProblemDetails) when its rewrite cannot reach Board.Api.
const PROXY_FAILURES = [500, 502, 503, 504];

async function readProblemTitle(response: Response): Promise<string> {
  try {
    const body: unknown = await response.json();
    if (typeof body === "object" && body !== null && "title" in body && typeof body.title === "string") {
      return body.title;
    }
  } catch {
    // body is not JSON: fall through to the status text
  }
  return response.statusText || "Request failed";
}

export async function fetchCards(fetchFn: FetchFn = fetch): Promise<CardsResult> {
  let response: Response;
  try {
    response = await fetchFn("/api/cards", { cache: "no-store" });
  } catch {
    return { ok: false, kind: "unreachable" };
  }

  if (!response.ok) {
    const proxyFailure = PROXY_FAILURES.includes(response.status) && !isProblemJson(response);
    if (proxyFailure) return { ok: false, kind: "unreachable" };
    return { ok: false, kind: "http", status: response.status, title: await readProblemTitle(response) };
  }

  let body: unknown;
  try {
    body = await response.json();
  } catch {
    return { ok: false, kind: "invalid" };
  }
  const cards = parseCards(body);
  return cards ? { ok: true, cards } : { ok: false, kind: "invalid" };
}

function isProblemJson(response: Response): boolean {
  return response.headers.get("content-type")?.includes("application/problem+json") ?? false;
}
