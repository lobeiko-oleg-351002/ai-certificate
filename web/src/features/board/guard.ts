import { CARD_STATUSES, type Card, type CardStatus } from "./types";

const KEY_PATTERN = /^BRD-[1-9][0-9]{0,8}$/;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isStatus(value: unknown): value is CardStatus {
  return CARD_STATUSES.some((status) => status === value);
}

function isNullableString(value: unknown): value is string | null {
  return value === null || typeof value === "string";
}

export function isCard(value: unknown): value is Card {
  return (
    isRecord(value) &&
    typeof value.key === "string" &&
    KEY_PATTERN.test(value.key) &&
    typeof value.title === "string" &&
    typeof value.description === "string" &&
    isStatus(value.status) &&
    isNullableString(value.phase) &&
    isNullableString(value.pendingGate) &&
    typeof value.createdAt === "string" &&
    typeof value.updatedAt === "string"
  );
}

export function parseCards(value: unknown): Card[] | null {
  return Array.isArray(value) && value.every(isCard) ? value : null;
}
