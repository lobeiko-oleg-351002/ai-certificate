import { describe, expect, it } from "vitest";
import { COLUMNS, groupByStatus } from "./columns";
import type { Card } from "./types";

const make = (key: string, status: Card["status"]): Card => ({
  key,
  title: key,
  description: "",
  status,
  phase: null,
  pendingGate: null,
  createdAt: "2026-11-03T10:00:00+00:00",
  updatedAt: "2026-11-03T10:00:00+00:00",
});

describe("columns", () => {
  it("has exactly three columns in board order", () => {
    expect(COLUMNS.map((c) => c.title)).toEqual(["To Do", "In Progress", "Done"]);
  });

  it("groups cards by status and keeps API order inside a column", () => {
    const groups = groupByStatus([make("BRD-1", "Done"), make("BRD-2", "ToDo"), make("BRD-3", "Done"), make("BRD-4", "InProgress")]);
    expect(groups.ToDo.map((c) => c.key)).toEqual(["BRD-2"]);
    expect(groups.InProgress.map((c) => c.key)).toEqual(["BRD-4"]);
    expect(groups.Done.map((c) => c.key)).toEqual(["BRD-1", "BRD-3"]);
  });

  it("returns empty groups for an empty board", () => {
    expect(groupByStatus([])).toEqual({ ToDo: [], InProgress: [], Done: [] });
  });
});
