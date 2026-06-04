import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      includeAssets: ["atlas-icon.svg"],
      manifest: {
        name: "Atlas ERP Autopecas",
        short_name: "Atlas ERP",
        start_url: "/",
        display: "standalone",
        background_color: "#f6f8fb",
        theme_color: "#0f766e",
        icons: [
          {
            src: "/atlas-icon.svg",
            sizes: "any",
            type: "image/svg+xml",
            purpose: "any maskable"
          }
        ]
      },
      workbox: {
        navigateFallback: "/",
        runtimeCaching: [
          {
            urlPattern: /^https?:\/\/localhost:5000\/api\//,
            handler: "NetworkFirst",
            options: {
              cacheName: "atlas-api",
              networkTimeoutSeconds: 4
            }
          }
        ]
      }
    })
  ],
  server: {
    port: 5173
  }
});
