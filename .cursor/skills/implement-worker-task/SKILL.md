---
name: implement-worker-task
description: Implements Worker tasks and batch file-processing flows for ExpenseFlow. Use when work involves BackgroundService, inbox polling, file scanning, deduplication, periodic jobs, moving files between inbox/processed/error, or batch execution logging.
---

# implement-worker-task

Use this skill when the task involves:
- `BackgroundService`
- folder polling
- batch processing
- file scanning
- deduplication
- moving files between `inbox` / `processed` / `error`
- periodic jobs
- batch execution logging

## Goal
Implement one Worker task in a constrained, safe way aligned with the project architecture.

## Project Context
ExpenseFlow processes purchase receipts from a local synchronized folder.

Architecture layers:
- Domain
- Application
- Infrastructure
- Worker
- Api

Worker responsibilities:
- inspect `inbox`
- select valid files
- invoke OCR through Application abstractions
- normalize data
- persist data
- move files to `processed` or `error`

## Required Workflow
1. Read first:
   - `AGENTS.md`
   - `docs/architecture.md`
   - the target task markdown file in `docs/tasks/`
2. Validate acceptance criteria before coding.
3. Implement only one task in this change.
4. Keep business logic out of Worker when it belongs to Application/Infrastructure.
5. Add useful structured logging for each batch:
   - job start
   - job end
   - detected file count
   - processed file count
   - failed file count
6. Use `CancellationToken` in async methods and loops.
7. Do not couple Worker directly to external SDKs.
8. If architecture or setup changes, update docs:
   - `docs/architecture.md` for architecture changes
   - `README.md` for setup/configuration changes

## Rules
- Do not mix more than one task.
- Do not do broad refactors.
- Do not put OCR logic directly inside the Worker loop.
- Do not hardcode paths or secrets.
- Put configuration in Options, `appsettings`, or environment variables.

## Implementation Checklist
Copy and track this checklist while implementing:

```md
Task Progress:
- [ ] Read `AGENTS.md`
- [ ] Read `docs/architecture.md`
- [ ] Read and confirm a single task markdown
- [ ] Confirm acceptance criteria
- [ ] Implement only that task
- [ ] Keep Worker thin (orchestration only)
- [ ] Add structured batch logs
- [ ] Ensure CancellationToken usage
- [ ] Keep external integrations behind interfaces
- [ ] Update docs if architecture/setup changed
- [ ] Verify build/tests relevant to change
```

## Expected Output
When done, return:
- change summary
- modified files
- risks or pending items
- confirmation of which acceptance criteria were covered
