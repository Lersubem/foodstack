import type { NextConfig } from "next";

const nextConfig: NextConfig = {
    output: "export",
    trailingSlash: true,
    ...(process.env.NODE_ENV === "production"
        ? { basePath: "/foodstack" }
        : {})
};

export default nextConfig;
