# Agent Workflow

All state is in the repository, so any new session can continue. One run processes one task, which keeps context small and failures isolated.

## Roles
| Role | Model | Why | Rights |
|---|---|---|---|
| dispatcher (main thread) | haiku | Follows fixed rules and checks objective signals only | Runs commands, updates board, merges; does not edit source code |
| developer | sonnet | Reliable implementation at moderate cost | Edits code, unit tests, changelog and devlog |
| tester | model set per task (haiku or sonnet) | Independent checks of acceptance criteria | Edits only `tests/` |
| security-reviewer | sonnet; opus at gate tasks | Stronger analysis only where risk is highest | Read-only plus running tests |

## Run
From the repository root on the development machine (needs .NET 10 SDK, SQL Server, `SWAB_TEST_SQL`):

```
claude --agent dispatcher "Run the next task"
```

Unattended single run:

```
claude -p --agent dispatcher "Run the next task"
```

## Flow
1. Dispatcher syncs `main`, and picks the first `Todo` task whose dependencies are `Done`.
2. It creates branch `task/T-XXX` and sets the task to `InProgress`.
3. Developer implements the task and commits.
4. Tester adds acceptance tests and reports `VERDICT: PASS` or `VERDICT: FAIL`.
5. Security-reviewer runs if the task requires it and reports the same verdict format.
6. On failure, the findings go back to the developer. After 2 fix rounds the task becomes `Blocked` and the run stops.
7. Dispatcher itself runs build and test commands. Agent reports alone are never enough.
8. Dispatcher merges to `main` with `--no-ff`, sets `Done`, pushes, and stops.

## Gates
The owner has delegated merging. T-015 and T-024 are mandatory Opus security reviews, and they merge automatically only on PASS. Blocked tasks stop for the owner. Destructive actions on existing data remain forbidden.

## Statuses
`Todo`, `InProgress`, `AwaitingApproval`, `Blocked`, `Done`. A stale `InProgress` task in a new session is resumed from its branch, never restarted over existing commits.

## Parallel work
Sequential by default, as it is the most stable and cheapest. Do not run two dispatchers at once.
