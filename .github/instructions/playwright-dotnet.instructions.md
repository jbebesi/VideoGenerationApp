# Playwright .NET Test Generation Instructions

## Test Writing
### Code Quality Standards

- **Locators**: Prioritize user-facing, role-based locators (`GetByRole`, `GetByLabel`, `GetByText`, etc.) for resilience and accessibility. Use `await Test.StepAsync()` to group interactions and improve test readability and reporting.
- **Assertions**: Use auto-retrying web-first assertions via `Expect()` (e.g., `await Expect(locator).ToHaveTextAsync()`). Avoid checking visibility unless specifically testing for visibility changes.
- **Timeouts**: Rely on Playwright's built-in auto-waiting mechanisms. Avoid hard-coded waits.

### Test Structure

- **Usings**: Start with `using Microsoft.Playwright;` and appropriate test framework usings (`Microsoft.Playwright.Xunit`, `Microsoft.Playwright.NUnit`, or `Microsoft.Playwright.MSTest`).
- **Organization**: Create test classes inheriting from `PageTest` or use fixtures. Group related tests for a feature in the same test class.
- **Setup**: Use `[SetUp]` (NUnit), `[TestInitialize]` (MSTest), or constructor initialization (xUnit).

### File Organization

- **Location**: Store test files in a `Tests/` directory or organize by feature.
- **Naming**: Use `<FeatureOrPage>Tests.cs` (e.g., `LoginTests.cs`).

### Assertion Best Practices

- **UI Structure**: Use `ToMatchAriaSnapshotAsync` to verify the accessibility tree.
- **Element Counts**: Use `ToHaveCountAsync`.
- **Text Content**: Use `ToHaveTextAsync` / `ToContainTextAsync`.
- **Navigation**: Use `ToHaveURLAsync` to verify navigation outcomes.

## Test Execution Strategy

1. **Initial Run**: Execute tests with `dotnet test`.
2. **Debug Failures**: Analyze failures and iterate.
3. **Validate**: Ensure tests pass consistently.

## Quality Checklist

- [ ] Locators are accessible and specific
- [ ] Tests follow naming conventions
- [ ] Assertions are meaningful and reflect user expectations

---
description: 'Playwright .NET test generation instructions'
applyTo:
'**'
