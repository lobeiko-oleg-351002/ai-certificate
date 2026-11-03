import type { NextConfig } from "next";

const boardApiUrl = process.env.BOARD_API_URL ?? "http://localhost:5080";

const nextConfig: NextConfig = {
  async rewrites() {
    return [{ source: "/api/:path*", destination: `${boardApiUrl}/api/:path*` }];
  },
};

export default nextConfig;
