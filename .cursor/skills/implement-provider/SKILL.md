---
name: implement-provider
description: Implements external providers behind Application interfaces using layered architecture. Use when tasks involve external integrations, Azure Document Intelligence, OCR providers, filesystem services, hashing, HTTP/SDK clients, or infrastructure adapters.
---

# implement-provider

Use this skill when the task involves:
- external integrations
- Azure Document Intelligence
- OCR providers
- filesystem services
- hashing services
- HTTP/SDK clients
- infrastructure adapters

## Objective
Build a clean, decoupled, and testable external integration.

## Mandatory Pattern
- Interface lives in Application
- Implementation lives in Infrastructure
- Configuration lives in typed Options
- Registration happens through DI
- The rest of the system depends on the interface, not the SDK

## Mandatory Checklist
1. Read:
   - `AGENTS.md`
   - `docs/architecture.md`
   - corresponding task markdown
2. Create or reuse an interface in Application
3. Implement provider in Infrastructure
4. Add strongly typed Options
5. Register provider in DI
6. Add error handling and structured logging
7. Return normalized output
8. Do not leak external SDK DTOs into Domain

## Rules
- Do not expose SDK types outside Infrastructure
- Do not hardcode endpoints, keys, or paths
- Do not map external outputs directly into Domain entities without an intermediate layer
- If credentials are required, use environment variables or local non-versioned config
- If full tests are not feasible, leave a fake or clear seam for testing

## Azure OCR Specifics
If the task is about Azure Document Intelligence:
- encapsulate the SDK client
- map receipt/invoice output into an internal DTO
- preserve `RawJson` or serialized original response when relevant
- return safe defaults when fields are missing
- log failures without leaking secrets

## Expected Output
When the task is done, report:
- interface used or created
- implementation added
- required configuration
- modified files
- limitations or assumptions
