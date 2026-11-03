"use client";

import { COLUMNS, groupByStatus } from "../columns";
import { useBoard } from "../queries";
import type { BoardProblem } from "../types";
import { Column } from "./Column";
import styles from "./Board.module.css";

function problemMessage(problem: BoardProblem): string {
  switch (problem.kind) {
    case "unreachable":
      return "Board unreachable: cannot reach Board.Api.";
    case "invalid":
      return "Invalid response from Board.Api.";
    case "http":
      return `Board.Api error ${problem.status}: ${problem.title}.`;
  }
}

export function Board() {
  const { cards, problem } = useBoard();

  if (cards === undefined && !problem) return <p role="status">Loading board...</p>;

  const banner = problem ? (
    <p role="alert" className={styles.banner}>
      {problemMessage(problem)}
      {cards ? " Showing the last known cards." : ""}
    </p>
  ) : null;
  if (!cards) return banner;

  const groups = groupByStatus(cards);
  return (
    <>
      {banner}
      <div className={styles.board}>
        {COLUMNS.map(({ status, title }) => (
          <Column key={status} title={title} cards={groups[status]} />
        ))}
      </div>
    </>
  );
}
