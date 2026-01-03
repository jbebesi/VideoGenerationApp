# .NET Upgrade Analysis Prompts

Project Discovery & Assessment
- Project Classification Analysis: Identify all projects in the solution and classify them by type (`.NET Framework`, `.NET Core`, `.NET Standard`).
- Dependency Compatibility Review: Review external and internal dependencies for framework compatibility.

Upgrade Strategy & Sequencing
- Recommend a project upgrade order and an incremental strategy planning with rollback checkpoints.

Framework Targeting & Code Adjustments
- Suggest correct `TargetFramework` for each project and recommend modern replacements for deprecated APIs.

CI/CD & Build Pipeline Updates
- Generate updated build pipeline snippets for .NET 8 migration and recommend validation builds.

Testing & Validation
- Propose validation checks to ensure upgraded solution builds and runs successfully.

---
name: ".NET Upgrade Analysis Prompts"
