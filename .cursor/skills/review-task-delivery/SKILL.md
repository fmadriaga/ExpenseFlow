---
name: review-task-delivery
description: Reviews whether a completed task delivery meets acceptance criteria and repository architecture constraints before approval or merge. Use when validating finished tasks, reviewing implementation scope, or deciding approve vs needs changes.
---

# review-task-delivery

Use this skill when a completed task must be reviewed before approval or merge.

## Objective
Validate that the change:
- meets acceptance criteria
- respects architecture
- maintains clean layer boundaries
- does not introduce obvious defects or debt

## Required Checklist
1. Read the corresponding task and its acceptance criteria.
2. Read `docs/architecture.md` and `AGENTS.md`.
3. Review modified files in the change.
4. Compare implementation against acceptance criteria.
5. Look for:
   - improper coupling between layers
   - missing or insufficient logging
   - weak error handling
   - hardcoded configuration values
   - out-of-scope side effects

## Expected Output
Always respond with:
- Critical findings
- Medium findings
- Suggested improvements
- Final decision: `approve` / `needs changes`
