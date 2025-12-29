# ZLGetCert Fixtures  
**Structure Standard – v1**

## Purpose

Fixtures are **proof artifacts**, not tests for vanity.

They exist to demonstrate that:

- The **contract is executable**
- **Doctor catches failures before damage**
- **Results are reproducible**
- Failures are **categorized, not vague**

If a future change breaks a fixture, the change is suspect.

---

## Fixture Rules

All fixtures must:

- Use explicit, committed input files
- Be runnable without UI
- Be explainable by reading output alone
- Map cleanly to a contract outcome

Fixtures **do not need to pass everywhere** — they must be *correct* where they are intended to run.

---

## Directory Layout

docs/fixtures/
  golden/
    request.json
    README.md
  bad-environment/
    request.json
    README.md
  export-path-failure/
    request.json
    README.md

Each fixture includes:
- request.json — CertificateRequest input
- README.md — intent, expectations, and notes

---

## Fixture 1 — Golden Path

### Name
Golden Path

### Purpose
Proves the happy path:
- Doctor passes
- Request succeeds
- Outputs are valid and parseable

### Files
docs/fixtures/golden/request.json
docs/fixtures/golden/README.md

### Expectations

1. Doctor:
   zlgetcert doctor --request docs/fixtures/golden/request.json
   - Exit code: 0
   - No failed checks

2. Request:
   zlgetcert request --request docs/fixtures/golden/request.json --format json
   - Exit code: 0
   - status = success
   - All requested exports written

3. Invariants:
   - Export paths writable
   - No overwrite violations
   - Leaf PEM parses
   - CA bundle contains ≥ 1 certificate
   - Private key exported only if explicitly requested

### Notes

- Intended for a known lab environment
- Uses a safe, short-lived test template
- Paths should point to a disposable directory

---

## Fixture 2 — Bad Environment

### Name
Bad Environment / Missing Capability

### Purpose
Proves Doctor's ability to detect environment failures early.

This fixture must fail before a certificate request is attempted.

### Files
docs/fixtures/bad-environment/request.json
docs/fixtures/bad-environment/README.md

### Example Scenarios (choose one)

Any one of the following is sufficient:

- CA hostname does not resolve
- CA host is unreachable
- Required privilege is missing (non-elevated run)
- Required tooling not found

### Expectations

1. Doctor:
   zlgetcert doctor --request docs/fixtures/bad-environment/request.json
   - Exit code: 2
   - At least one failed check

2. Failure:
   - failureCategory:
     - EnvironmentError or ConnectivityError
   - Clear remediation instructions

3. Request:
   - Must not be attempted
   - If attempted, it must fail fast with the same category

### Notes

- This fixture exists to prevent just try it and see
- Output should clearly explain why it failed and how to fix it

---

## Fixture 3 — Export Path Failure

### Name
Export Path Failure

### Purpose
Proves ZLGetCert never writes blindly.

This fixture validates:
- Path validation
- Overwrite protection
- Export error categorization

### Files
docs/fixtures/export-path-failure/request.json
docs/fixtures/export-path-failure/README.md

### Example Scenarios

One of:
- Export path parent directory does not exist
- Export path is not writable
- Export file already exists and overwrite = false

### Expectations

1. Doctor:
   zlgetcert doctor --request docs/fixtures/export-path-failure/request.json
   - Exit code: 2
   - Failure category: ExportError
   - Evidence includes:
     - Resolved absolute path
     - Exists / writable / overwrite analysis

2. Request behavior:
   - Preferred: Doctor blocks request
   - Acceptable: Request succeeds but export fails with ExportError
   - Never acceptable: Silent overwrite or partial write without error

### Notes

- This fixture enforces the no surprises rule
- Export safety is non-negotiable

---

## Fixture Validation Matrix

| Fixture | Doctor | Request | Category |
|------|--------|---------|----------|
| Golden Path | Pass | Success | — |
| Bad Environment | Fail | Blocked | EnvironmentError / ConnectivityError |
| Export Path Failure | Fail | Blocked or ExportFail | ExportError |

---

## Maintenance Rules

- Fixtures must be updated only if:
  - The contract changes
  - The behavior is intentionally redefined
- Breaking a fixture without updating documentation is a regression

---

## Authority Statement

These fixtures are living proof of the ZLGetCert contract.

If:
- Code and fixtures disagree → fixtures win
- Behavior and fixtures disagree → fixtures win
- UI and fixtures disagree → fixtures win

Fixtures are not optional.

---

## Design Intent

Fixtures exist so ZLGetCert can be trusted:

- In automation
- In OT environments
- By operators who can't afford trial and error

If a fixture fails unexpectedly, stop and investigate.
