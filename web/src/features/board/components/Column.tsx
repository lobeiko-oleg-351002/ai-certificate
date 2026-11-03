import type { Card } from "../types";
import { CardItem } from "./CardItem";
import styles from "./Board.module.css";

export function Column({ title, cards }: { title: string; cards: Card[] }) {
  return (
    <section className={styles.column} aria-label={title}>
      <h2 className={styles.columnTitle}>
        {title} <span className={styles.count}>{cards.length}</span>
      </h2>
      {cards.length === 0 ? (
        <p className={styles.empty}>No cards</p>
      ) : (
        <ul className={styles.list}>
          {cards.map((card) => (
            <CardItem key={card.key} card={card} />
          ))}
        </ul>
      )}
    </section>
  );
}
