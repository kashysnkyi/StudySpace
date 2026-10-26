import { expect, test } from "@playwright/test";

test("landing page provides a clear path to workspaces", async ({ page }) => {
  await page.goto("/");
  await expect(page).toHaveTitle(/StudySpace/);
  await expect(page.getByRole("heading", { name: /Нотатки, файли та командна робота/i })).toBeVisible();
  await page.getByRole("link", { name: /простори/i }).first().click();
  await expect(page.getByRole("heading", { name: "Робочі простори" })).toBeVisible();
});

test("autocomplete opens the seeded document", async ({ page }) => {
  await page.goto("/Workspaces");
  await page.getByPlaceholder("Пошук документів…").fill("Ласкаво");
  await expect(page.locator("#searchResults")).toContainText("Ласкаво просимо до StudySpace");
  await page.locator("#searchResults a").first().click();
  await expect(page.getByRole("heading", { name: "Ласкаво просимо до StudySpace" })).toBeVisible();
});

test("block editor exposes realtime editing controls", async ({ page }) => {
  await page.goto("/Documents/Details/5ded85a3-035d-46af-934e-5c1c204a4ec7");
  await page.getByRole("link", { name: /Відкрити редактор/i }).click();
  await expect(page.locator(".editor-shell[data-document-id]")).toBeVisible();
  await expect(page.getByText(/Realtime підключено|Підключення/i)).toBeVisible();
  await expect(page.locator("[data-block-id]")).toHaveCount(3);
});

test("map loads API-backed workspace markers", async ({ page }) => {
  await page.goto("/Map");
  await expect(page.locator("#workspaceMap")).toBeVisible();
  await expect(page.locator(".leaflet-marker-icon")).toHaveCount(1);
});

test("SEO endpoints are public", async ({ request }) => {
  const robots = await request.get("/robots.txt");
  expect(robots.ok()).toBeTruthy();
  expect(await robots.text()).toContain("Sitemap:");
  const sitemap = await request.get("/sitemap.xml");
  expect(sitemap.ok()).toBeTruthy();
  expect(await sitemap.text()).toContain("urlset");
});
