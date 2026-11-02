# Task Board

Updated by the dispatcher. Order is the execution order.

| ID | Title | Depends on | Status | Test model | Review | Human gate |
|---|---|---|---|---|---|---|
| [T-001](T-001.md) | Solution scaffold and CI | - | InProgress | haiku | none | no |
| [T-002](T-002.md) | Database foundation and schema | T-001 | Todo | haiku | none | no |
| [T-003](T-003.md) | Structured logging and error handling | T-001 | Todo | haiku | none | no |
| [T-004](T-004.md) | Security pipeline baseline | T-001, T-003 | Todo | sonnet | sonnet | no |
| [T-005](T-005.md) | Turkish layout and localization | T-001 | Todo | haiku | none | no |
| [T-006](T-006.md) | Identity with Dapper stores | T-002, T-004 | Todo | sonnet | sonnet | no |
| [T-007](T-007.md) | E-mail service | T-001, T-003 | Todo | haiku | none | no |
| [T-008](T-008.md) | Registration and e-mail confirmation | T-005, T-006, T-007 | Todo | sonnet | sonnet | no |
| [T-009](T-009.md) | Login, logout and lockout | T-008 | Todo | sonnet | sonnet | no |
| [T-010](T-010.md) | Password recovery and profile | T-009 | Todo | sonnet | sonnet | no |
| [T-011](T-011.md) | Admin area and bootstrap command | T-009 | Todo | sonnet | sonnet | no |
| [T-012](T-012.md) | Two-factor authentication | T-010 | Todo | sonnet | sonnet | no |
| [T-013](T-013.md) | E-mail change and session control | T-010 | Todo | sonnet | sonnet | no |
| [T-014](T-014.md) | Admin user management | T-011, T-012 | Todo | sonnet | sonnet | no |
| [T-015](T-015.md) | Security gate A: account system | T-010, T-011, T-012, T-013, T-014 | Todo | haiku | opus | no |
| [T-016](T-016.md) | Site settings and branding resolver | T-015 | Todo | haiku | sonnet | no |
| [T-017](T-017.md) | Branding portal and secure upload | T-016 | Todo | sonnet | sonnet | no |
| [T-018](T-018.md) | Log level portal | T-016, T-003 | Todo | haiku | sonnet | no |
| [T-019](T-019.md) | Editable legal documents | T-016 | Todo | sonnet | sonnet | no |
| [T-020](T-020.md) | Consent records and cookie notice | T-019, T-008 | Todo | sonnet | sonnet | no |
| [T-021](T-021.md) | Data subject rights | T-015, T-020 | Todo | sonnet | sonnet | no |
| [T-022](T-022.md) | Announcement management | T-015 | Todo | haiku | sonnet | no |
| [T-023](T-023.md) | Announcement reading and replies | T-022 | Todo | sonnet | sonnet | no |
| [T-024](T-024.md) | Security gate B and release readiness | T-017, T-018, T-020, T-021, T-023 | Todo | sonnet | opus | no |
