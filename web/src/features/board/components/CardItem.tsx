import type { Card } from "../types";
import styles from "./Board.module.css";

export function CardItem({ card }: { card: Card }) {
  return (
    <li className={styles.card}>
      <p className={styles.key}>{card.key}</p>
      <h3 className={styles.cardTitle}>{card.title}</h3>
      {card.description ? <p className={styles.description}>{card.description}</p> : null}
      <div className={styles.meta}>
        <span>Status: {card.status}</span>
        {card.phase ? <span className={styles.phase}>{card.phase}</span> : null}
      </div>
      {card.pendingGate ? (
        <p className={styles.gate} role="note" aria-label={`Pending gate: ${card.pendingGate}`}>
          Gate: {card.pendingGate}
        </p>
      ) : null}
    </li>
  );
}
