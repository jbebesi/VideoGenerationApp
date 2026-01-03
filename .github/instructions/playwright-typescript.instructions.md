## Playwright TypeScript Test Generation

### Code Quality Standards
- Prioritize user-facing locators (`getByRole`, `getByLabel`, etc.) and use `test.step()` to group interactions.
- Use auto-retrying web-first assertions and avoid hard-coded waits.

### Test Structure
- Start with `import { test, expect } from '@playwright/test';` and group related tests with `test.describe()`.

---
description: 'Playwright test generation instructions'
applyTo: '**'
