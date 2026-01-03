# XUnit Best Practices

## Project Setup

- Use a separate test project with naming convention `[ProjectName].Tests`
- Reference Microsoft.NET.Test.Sdk, xunit, and xunit.runner.visualstudio packages
- Create test classes that match the classes being tested
- Use the Arrange-Act-Assert (AAA) pattern
- Name tests using the pattern `MethodName_Scenario_ExpectedBehavior`

## Test Structure

- No test class attributes required (unlike MSTest/NUnit)
- Use `[Fact]` for simple tests and `[Theory]` for data-driven
- Follow AAA pattern
- Use constructor for setup and `IDisposable.Dispose()` for teardown

---
agent: 'agent'
