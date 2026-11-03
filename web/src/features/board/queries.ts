import { useQuery } from "@tanstack/react-query";
import { fetchCards } from "./api";
import type { BoardProblem, Card } from "./types";

export const POLL_INTERVAL_MS = 2000;

export const boardKeys = {
  all: ["board"] as const,
  cards: () => [...boardKeys.all, "cards"] as const,
};

export type BoardState = {
  cards: Card[] | undefined;
  problem: BoardProblem | null;
};

class BoardProblemError extends Error {
  constructor(readonly problem: BoardProblem) {
    super(problem.kind);
  }
}

// A failed poll throws so the query keeps the last good cards in `data` and exposes the failure in `error`.
async function loadCards(): Promise<Card[]> {
  const result = await fetchCards();
  if (!result.ok) throw new BoardProblemError(result);
  return result.cards;
}

export function useBoard(): BoardState {
  const { data, error } = useQuery({
    queryKey: boardKeys.cards(),
    queryFn: loadCards,
    refetchInterval: POLL_INTERVAL_MS,
  });
  const problem: BoardProblem | null =
    error instanceof BoardProblemError ? error.problem : error ? { ok: false, kind: "unreachable" } : null;
  return { cards: data, problem };
}
