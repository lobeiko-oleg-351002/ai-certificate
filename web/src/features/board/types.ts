export const CARD_STATUSES = ["ToDo", "InProgress", "Done"] as const;

export type CardStatus = (typeof CARD_STATUSES)[number];

export type Card = {
  key: string;
  title: string;
  description: string;
  status: CardStatus;
  phase: string | null;
  pendingGate: string | null;
  createdAt: string;
  updatedAt: string;
};

export type CardsResult =
  | { ok: true; cards: Card[] }
  | { ok: false; kind: "unreachable" }
  | { ok: false; kind: "http"; status: number; title: string }
  | { ok: false; kind: "invalid" };

export type BoardProblem = Exclude<CardsResult, { ok: true }>;
