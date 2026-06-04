import { expect, test } from "@playwright/test";

test("renders installable ERP shell", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByText("Atlas ERP")).toBeVisible();
  await expect(page.getByRole("button", { name: /Entrar|Sair/ })).toBeVisible();
});
