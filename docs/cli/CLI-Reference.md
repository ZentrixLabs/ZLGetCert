# ZLGetCert CLI Reference  
**Structure Standard – v1**

## Purpose

The ZLGetCert CLI is the **canonical reference client**.

All behavior exposed by:
- UI
- automation
- scripts
- future integrations

must be achievable via the CLI, and must obey the same contract.

If something cannot be done via the CLI, it is not a real capability.

---

## Global CLI Principles

1. Explicit input
   - No interactive prompts
   - No inferred paths
   - No hidden defaults that affect outputs

2. Stable output
   - Human-readable text for operators
   - Machine-readable JSON for automation

3. Deterministic exit codes
   - Scripts must be able to trust outcomes

4. Contract-first
   - All CLI outputs map to CertificateResult
   - All failures map to contract failure categories

---

## Command Overview

zlgetcert <command> [options]

### Supported Commands (v1)

- doctor — preflight diagnostics (read-only)
- request — certificate request execution

Future commands may exist, but these two define the core authority surface.

---

## Common Options

These options are supported by all commands unless stated otherwise.

| Option | Description |
|------|------------|
| --format text|json | Output format (default: text) |
| --request <path> | Path to CertificateRequest JSON |
| --config <path> | Optional config override path |

---

## Exit Codes (Global)

| Code | Meaning |
|----|--------|
| 0 | Success |
| 1 | Execution failure |
| 2 | Validation / preflight failure |

Exit codes are stable and script-safe.

---

## zlgetcert doctor

### Description

Runs all preflight checks required to safely execute a certificate request.

Doctor:
- Performs no writes
- Issues no certificates
- Modifies no system state

### Syntax

zlgetcert doctor
  --request <request.json>
  [--format text|json]

### Behavior

- Parses the request
- Validates configuration
- Verifies environment readiness
- Verifies export destinations
- Verifies CA reachability (non-mutating)

### Output

Text Mode

- Sectioned report:
  - Environment
  - Configuration
  - Export Destinations
  - CA Reachability
- Each check marked:
  - pass
  - fail
  - warn
- Failure includes:
  - category
  - explanation
  - remediation

JSON Mode

Returns a structured DoctorResult object:

{
  "status": "fail",
  "passed": 14,
  "failed": 1,
  "warnings": 0,
  "checks": [
    {
      "id": "export.path.writable",
      "status": "fail",
      "category": "ExportError",
      "summary": "Export path is not writable",
      "evidence": {
        "resolvedPath": "C:\\certs\\leaf.pem",
        "exists": true,
        "overwriteAllowed": false
      },
      "remediation": "Choose a writable path or enable overwrite"
    }
  ]
}

### Exit Code Rules

- 0 — all checks passed
- 2 — one or more checks failed

---

## zlgetcert request

### Description

Executes a certificate request only if all contract requirements are met.

Internally, request execution must satisfy the same invariants enforced by Doctor.

### Syntax

zlgetcert request
  --request <request.json>
  [--format text|json]

### Execution Flow

1. Parse request
2. Validate configuration
3. Enforce invariants
4. Execute CA request
5. Export requested artifacts
6. Emit CertificateResult

If any step fails, execution stops and a structured failure is returned.

---

### Output

Text Mode

- Clear summary:
  - Success or failure
  - CA response status
  - What was written and where
- Explicit warnings for sensitive actions:
  - Private key export

Example:

✔ Certificate issued successfully
  CN: example.internal
  Valid: 2025-03-01 → 2026-03-01

Artifacts:
  ✔ leaf.pem → C:\certs\leaf.pem
  ✔ ca-bundle.pem → C:\certs\ca-bundle.pem
  ⚠ private key exported (explicitly requested)

JSON Mode (Authoritative)

Always emits a CertificateResult object.

Example (failure):

{
  "status": "failed",
  "failureCategory": "CARequestError",
  "message": "Certificate request rejected by CA",
  "requestId": "lab-test-001",
  "timestampUtc": "2025-03-01T22:41:12Z"
}

JSON output is:
- Stable
- Parsable
- Backwards-compatible within major versions

---

## Validation & Failure Behavior

Validation Failures

Examples:
- Missing required fields
- Illegal configuration combinations
- Invalid SAN entries

Result:
- Command fails fast
- Exit code 2
- failureCategory = ConfigurationError

---

Execution Failures

Examples:
- CA unreachable
- Enrollment rejected
- Export write failure

Result:
- Exit code 1
- Structured CertificateResult
- Accurate failureCategory

---

## Non-Interactive Rule

The CLI must never:

- Prompt for missing input
- Ask confirmation questions
- Pause for user decisions

If required input is missing, the command fails with a clear error.

---

## Authority Statement

This CLI defines what ZLGetCert is allowed to do.

Any behavior:
- Present in UI
- Present in scripts
- Present in future services

must map directly to a CLI command with identical semantics.

If behavior diverges, the CLI wins.

---

## Versioning

- CLI behavior is versioned implicitly with the contract
- Breaking changes require:
  - Contract version bump
  - Explicit documentation

---

## Design Intent

The CLI exists so ZLGetCert can be:

- Automated safely
- Audited easily
- Used in OT / legacy environments
- Trusted without reading the source

If a user can't understand what happened by reading CLI output alone, the CLI is incomplete.
