import { describe, expect, it } from "vitest";
import { isCard, parseCards } from "./guard";

const validCard = {
  key: "BRD-1",
  title: "Add board API",
  description: "",
  status: "ToDo",
  phase: null,
  pendingGate: null,
  createdAt: "2026-11-03T10:00:00+00:00",
  updatedAt: "2026-11-03T10:00:00+00:00",
};

describe("isCard", () => {
  it("accepts a card with null phase and pendingGate", () => {
    expect(isCard(validCard)).toBe(true);
  });

  it("accepts a card with phase and pendingGate set", () => {
    expect(isCard({ ...validCard, phase: "plan", pendingGate: "plan" })).toBe(true);
  });

  it.each(["ToDo", "InProgress", "Done"])("accepts status %s", (status) => {
    expect(isCard({ ...validCard, status })).toBe(true);
  });

  it.each([
    ["unknown status", { status: "Blocked" }],
    ["numeric status", { status: 1 }],
    ["key without prefix", { key: "1" }],
    ["zero-padded key", { key: "BRD-01" }],
    ["key with more than nine digits", { key: "BRD-1234567890" }],
    ["non-string title", { title: 5 }],
    ["missing description", { description: undefined }],
    ["undefined phase", { phase: undefined }],
    ["numeric updatedAt", { updatedAt: 1 }],
  ])("rejects %s", (_name, override) => {
    expect(isCard({ ...validCard, ...override })).toBe(false);
  });

  it.each([null, undefined, "card", 3, []])("rejects non-object %j", (value) => {
    expect(isCard(value)).toBe(false);
  });
});

describe("parseCards", () => {
  it("returns an empty list for an empty array", () => {
    expect(parseCards([])).toEqual([]);
  });

  it("returns the cards when all are valid", () => {
    expect(parseCards([validCard])).toEqual([validCard]);
  });

  it("returns null when one card is invalid", () => {
    expect(parseCards([validCard, { ...validCard, status: "x" }])).toBeNull();
  });

  it("returns null when the body is not an array", () => {
    expect(parseCards({ cards: [] })).toBeNull();
  });
});
