import { expect, test } from "@playwright/test";

test("renders desktop ERP shell", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByText("AutoParts ERP")).toBeVisible();
  await expect(page.getByRole("button", { name: /Entrar|Sair/ })).toBeVisible();
});
