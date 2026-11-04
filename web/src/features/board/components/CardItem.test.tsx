import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { Card } from "../types";
import { CardItem } from "./CardItem";

const card = (extra: Partial<Card> = {}): Card => ({
  key: "BRD-5",
  title: "Catch-up",
  description: "",
  status: "InProgress",
  phase: null,
  pendingGate: null,
  createdAt: "2026-11-03T10:00:00+00:00",
  updatedAt: "2026-11-03T10:00:00+00:00",
  ...extra,
});

const renderCard = (extra?: Partial<Card>) =>
  render(
    <ul>
      <CardItem card={card(extra)} />
    </ul>,
  );

describe("CardItem phase and gate", () => {
  it("shows neither badge nor marker when phase and gate are unset", () => {
    renderCard();
    expect(screen.getByText("Status: InProgress")).toBeInTheDocument();
    expect(screen.queryByText(/^Gate:/)).toBeNull();
    expect(screen.queryByText("implement")).toBeNull();
  });

  it("shows the phase badge with the phase text", () => {
    renderCard({ phase: "implement" });
    expect(screen.getByText("implement")).toBeInTheDocument();
    expect(screen.queryByText(/^Gate:/)).toBeNull();
  });

  it("shows the gate marker while a gate is pending", () => {
    renderCard({ pendingGate: "plan" });
    expect(screen.getByText("Gate: plan")).toBeInTheDocument();
    expect(screen.getByRole("note", { name: "Pending gate: plan" })).toBeInTheDocument();
  });

  it("shows both together and stays read-only", () => {
    renderCard({ phase: "plan", pendingGate: "plan" });
    expect(screen.getByText("plan", { selector: "span" })).toBeInTheDocument();
    expect(screen.getByText("Gate: plan")).toBeInTheDocument();
    expect(screen.queryByRole("button")).toBeNull();
  });
});
