import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./specs",
  timeout: 30_000,
  fullyParallel: false,
  reporter: [["html", { open: "never" }], ["list"]],
  use: { baseURL: "http://127.0.0.1:5036", trace: "retain-on-failure", screenshot: "only-on-failure" },
  webServer: {
    command: "dotnet run --project ../../src/StudySpace.Web --no-build --urls http://127.0.0.1:5036",
    url: "http://127.0.0.1:5036",
    reuseExistingServer: true,
    timeout: 120_000
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }]
});
