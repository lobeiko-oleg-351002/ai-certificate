import type { Card } from "../types";
import styles from "./Board.module.css";

export function CardItem({ card }: { card: Card }) {
  return (
    <li className={styles.card}>
      <p className={styles.key}>{card.key}</p>
      <h3 className={styles.cardTitle}>{card.title}</h3>
      {card.description ? <p className={styles.description}>{card.description}</p> : null}
      <p className={styles.meta}>
        Status: {card.status}
        {card.phase ? ` | Phase: ${card.phase}` : ""}
        {card.pendingGate ? ` | Gate: ${card.pendingGate}` : ""}
      </p>
    </li>
  );
}
