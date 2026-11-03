"use client";

export default function Error({ reset }: { reset: () => void }) {
  return (
    <main>
      <h1>Personal Task Board</h1>
      <p role="alert">The board failed to render.</p>
      <button type="button" onClick={reset}>
        Try again
      </button>
    </main>
  );
}
