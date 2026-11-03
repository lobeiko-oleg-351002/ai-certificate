import type { Metadata } from "next";
import type { ReactNode } from "react";
import { QueryProvider } from "@/shared/query-provider";
import "./globals.css";

export const metadata: Metadata = {
  title: "Personal Task Board",
  description: "Read-only view of the task board",
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body>
        <QueryProvider>{children}</QueryProvider>
      </body>
    </html>
  );
}
