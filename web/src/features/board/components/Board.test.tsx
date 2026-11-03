import { focusManager, QueryClientProvider } from "@tanstack/react-query";
import { act, render, screen, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { createQueryClient } from "@/shared/query-provider";
import { POLL_INTERVAL_MS } from "../queries";
import type { Card } from "../types";
import { Board } from "./Board";

const make = (key: string, status: Card["status"], extra: Partial<Card> = {}): Card => ({
  key,
  title: `Title ${key}`,
  description: `Description ${key}`,
  status,
  phase: null,
  pendingGate: null,
  createdAt: "2026-11-03T10:00:00+00:00",
  updatedAt: "2026-11-03T10:00:00+00:00",
  ...extra,
});

const json = (body: unknown) => async () => new Response(JSON.stringify(body), { status: 200 });
const sequence = (...replies: Array<() => Promise<Response>>) => {
  let call = 0;
  return () => replies[Math.min(call++, replies.length - 1)]!();
};
const down = () => async () => new Response("Internal Server Error", { status: 500 });

function renderBoard() {
  return render(
    <QueryClientProvider client={createQueryClient()}>
      <Board />
    </QueryClientProvider>,
  );
}

async function advance(ms: number) {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(ms);
    // Response.json() settles on a later tick than the timer that started the request.
    await vi.advanceTimersByTimeAsync(50);
  });
}

describe("Board", () => {
  const fetchMock = vi.fn<typeof fetch>();

  beforeEach(() => {
    vi.useFakeTimers();
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it("shows a loading state before the first response", () => {
    fetchMock.mockReturnValue(new Promise(() => {}));
    renderBoard();
    expect(screen.getByRole("status")).toHaveTextContent("Loading board");
  });

  it("renders exactly three columns in order with the cards in the right one", async () => {
    fetchMock.mockImplementation(
      json([
        make("BRD-1", "Done"),
        make("BRD-2", "InProgress", { phase: "implement", pendingGate: "plan" }),
        make("BRD-3", "ToDo"),
      ]),
    );
    renderBoard();
    await advance(0);

    const columns = screen.getAllByRole("region");
    expect(columns.map((c) => c.getAttribute("aria-label"))).toEqual(["To Do", "In Progress", "Done"]);

    const inProgress = within(columns[1]!);
    expect(inProgress.getByText("BRD-2")).toBeInTheDocument();
    expect(inProgress.getByText("Title BRD-2")).toBeInTheDocument();
    expect(inProgress.getByText("Description BRD-2")).toBeInTheDocument();
    expect(inProgress.getByText(/Status: InProgress \| Phase: implement \| Gate: plan/)).toBeInTheDocument();
    expect(within(columns[0]!).getByText("BRD-3")).toBeInTheDocument();
    expect(within(columns[2]!).getByText("BRD-1")).toBeInTheDocument();
  });

  it("shows empty columns for an empty board", async () => {
    fetchMock.mockImplementation(json([]));
    renderBoard();
    await advance(0);
    expect(screen.getAllByText("No cards")).toHaveLength(3);
  });

  it("renders card text as text, not as markup", async () => {
    fetchMock.mockImplementation(json([make("BRD-1", "ToDo", { title: "<img src=x onerror=alert(1)>" })]));
    renderBoard();
    await advance(0);
    expect(screen.getByText("<img src=x onerror=alert(1)>")).toBeInTheDocument();
    expect(document.querySelector("img")).toBeNull();
  });

  it("has no interactive controls", async () => {
    fetchMock.mockImplementation(json([make("BRD-1", "ToDo")]));
    renderBoard();
    await advance(0);
    expect(screen.queryByRole("button")).toBeNull();
    expect(screen.queryByRole("textbox")).toBeNull();
  });

  it("picks up a status change on the next poll without any user action", async () => {
    fetchMock.mockImplementation(sequence(json([make("BRD-1", "ToDo")]), json([make("BRD-1", "InProgress")])));
    renderBoard();
    await advance(0);
    expect(within(screen.getByRole("region", { name: "To Do" })).getByText("BRD-1")).toBeInTheDocument();

    await advance(POLL_INTERVAL_MS);
    expect(within(screen.getByRole("region", { name: "In Progress" })).getByText("BRD-1")).toBeInTheDocument();
    expect(within(screen.getByRole("region", { name: "To Do" })).queryByText("BRD-1")).toBeNull();
  });

  it("keeps the last cards and shows a banner when a poll fails, then clears it on recovery", async () => {
    fetchMock.mockImplementation(sequence(json([make("BRD-1", "ToDo")]), down(), json([make("BRD-1", "Done")])));
    renderBoard();
    await advance(0);

    await advance(POLL_INTERVAL_MS);
    expect(screen.getByRole("alert")).toHaveTextContent("Board unreachable");
    expect(within(screen.getByRole("region", { name: "To Do" })).getByText("BRD-1")).toBeInTheDocument();

    await advance(POLL_INTERVAL_MS);
    expect(screen.queryByRole("alert")).toBeNull();
    expect(within(screen.getByRole("region", { name: "Done" })).getByText("BRD-1")).toBeInTheDocument();
  });

  it("shows the banner instead of an empty board when the first load fails", async () => {
    fetchMock.mockImplementation(down());
    renderBoard();
    await advance(0);
    expect(screen.getByRole("alert")).toHaveTextContent("Board unreachable");
    expect(screen.queryAllByRole("region")).toHaveLength(0);
  });

  it("reports an invalid response", async () => {
    fetchMock.mockImplementation(json([{ key: "nope" }]));
    renderBoard();
    await advance(0);
    expect(screen.getByRole("alert")).toHaveTextContent("Invalid response");
  });

  it("does not start a second request while one is in flight", async () => {
    fetchMock.mockReturnValue(new Promise(() => {}));
    renderBoard();
    await advance(POLL_INTERVAL_MS * 3);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("pauses polling while the tab is hidden and refreshes when it becomes visible", async () => {
    fetchMock.mockImplementation(json([]));
    renderBoard();
    await advance(0);
    expect(fetchMock).toHaveBeenCalledTimes(1);

    act(() => focusManager.setFocused(false));
    await advance(POLL_INTERVAL_MS * 3);
    expect(fetchMock).toHaveBeenCalledTimes(1);

    act(() => focusManager.setFocused(true));
    await advance(0);
    expect(fetchMock).toHaveBeenCalledTimes(2);
    act(() => focusManager.setFocused(undefined));
  });

  it("polls every interval and stops after unmount", async () => {
    fetchMock.mockImplementation(json([]));
    const view = renderBoard();
    await advance(0);
    await advance(POLL_INTERVAL_MS);
    await advance(POLL_INTERVAL_MS);
    expect(fetchMock).toHaveBeenCalledTimes(3);

    view.unmount();
    await advance(POLL_INTERVAL_MS * 3);
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });
});
