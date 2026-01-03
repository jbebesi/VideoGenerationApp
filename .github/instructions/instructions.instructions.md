## Custom Instructions

## Project Context

- Target audience: Developers and GitHub Copilot working with domain-specific code
- File format: Markdown with YAML frontmatter
- Location: `.github/instructions/` directory

## Required Frontmatter

Every instruction file must include YAML frontmatter with the following fields:

```yaml
---
description: 'Brief description of the instruction purpose and scope'
applyTo: 'glob pattern for target files (e.g., **/*.ts, **/*.py)'
---
```

### Frontmatter Guidelines

- **description**: Single-quoted string, 1-500 characters, clearly stating the purpose
- **applyTo**: Glob pattern(s) specifying which files these instructions apply to

## File Structure

- Title and Overview
- Core Sections: General Instructions, Best Practices, Code Standards, Examples

## Example Structure

```markdown
---
description: 'Brief description of purpose'
applyTo: '**/*.ext'
---

# Technology Name Development

## General Instructions

- High-level guideline 1
- High-level guideline 2

## Best Practices

- Specific practice 1

## Code Standards

### Naming Conventions
- Rule 1

## Validation
- Build command: `command to verify`
```

---
description: 'Guidelines for creating high-quality custom instruction files for GitHub Copilot'
applyTo: '**/*.instructions.md'
