import type { Card, CardStatus } from "./types";

export const COLUMNS: ReadonlyArray<{ status: CardStatus; title: string }> = [
  { status: "ToDo", title: "To Do" },
  { status: "InProgress", title: "In Progress" },
  { status: "Done", title: "Done" },
];

export function groupByStatus(cards: Card[]): Record<CardStatus, Card[]> {
  const groups: Record<CardStatus, Card[]> = { ToDo: [], InProgress: [], Done: [] };
  for (const card of cards) groups[card.status].push(card);
  return groups;
}
