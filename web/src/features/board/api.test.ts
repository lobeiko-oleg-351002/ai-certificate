import { describe, expect, it } from "vitest";
import { fetchCards } from "./api";

const card = {
  key: "BRD-2",
  title: "UI",
  description: "read-only",
  status: "InProgress",
  phase: "implement",
  pendingGate: null,
  createdAt: "2026-11-03T10:00:00+00:00",
  updatedAt: "2026-11-03T10:05:00+00:00",
};

const respond = (body: BodyInit | null, init: ResponseInit): typeof fetch => async () => new Response(body, init);

describe("fetchCards", () => {
  it("returns the cards for a valid 200", async () => {
    const result = await fetchCards(respond(JSON.stringify([card]), { status: 200 }));
    expect(result).toEqual({ ok: true, cards: [card] });
  });

  it("returns an empty list for an empty board", async () => {
    const result = await fetchCards(respond("[]", { status: 200 }));
    expect(result).toEqual({ ok: true, cards: [] });
  });

  it("reports invalid for a 200 with a malformed card", async () => {
    const result = await fetchCards(respond(JSON.stringify([{ ...card, status: "Blocked" }]), { status: 200 }));
    expect(result).toEqual({ ok: false, kind: "invalid" });
  });

  it("reports invalid for a 200 that is not JSON", async () => {
    const result = await fetchCards(respond("<html>", { status: 200 }));
    expect(result).toEqual({ ok: false, kind: "invalid" });
  });

  it("maps a ProblemDetails 500 to http with the title", async () => {
    const problem = respond(JSON.stringify({ title: "An error occurred", status: 500 }), {
      status: 500,
      headers: { "content-type": "application/problem+json" },
    });
    expect(await fetchCards(problem)).toEqual({ ok: false, kind: "http", status: 500, title: "An error occurred" });
  });

  it("maps a bare proxy 500 (backend down) to unreachable", async () => {
    const result = await fetchCards(respond("Internal Server Error", { status: 500 }));
    expect(result).toEqual({ ok: false, kind: "unreachable" });
  });

  it("maps a network failure to unreachable", async () => {
    const result = await fetchCards(async () => {
      throw new TypeError("fetch failed");
    });
    expect(result).toEqual({ ok: false, kind: "unreachable" });
  });

  it("maps a non-proxy error status to http", async () => {
    const result = await fetchCards(respond("", { status: 404, statusText: "Not Found" }));
    expect(result).toEqual({ ok: false, kind: "http", status: 404, title: "Not Found" });
  });
});
