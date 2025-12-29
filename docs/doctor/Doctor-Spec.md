# ZLGetCert Doctor  
**Structure Standard – v1**

## Purpose

`zlgetcert doctor` is a **read-only diagnostic surface**.

Its job is to detect **configuration, environment, connectivity, and export failures** *before* a certificate request is attempted.

Doctor **must not**:
- Mint certificates
- Write output files
- Modify system state

Doctor **must**:
- Be explainable
- Be deterministic
- Catch the most common failures early
- Map every failure to a contract failure category

If Doctor passes, a request is *allowed* to run.  
If Doctor fails, a request **must not proceed**.

---

## Command

zlgetcert doctor
  [--request <request.json>]
  [--config <config.json>]
  [--format text|json]

### Exit Codes

- 0 — All checks passed
- 2 — One or more checks failed (actionable failure)

---

## Output Model

Doctor produces a structured report composed of **checks**.

Each check includes:

- id — stable identifier
- status — pass | fail | warn
- category — contract failure category (only for fail)
- summary — short human-readable result
- detail — expanded explanation
- evidence — concrete facts (paths, versions, resolved values)
- remediation — how to fix

### Output Formats

- Text — human-readable, grouped by section
- JSON — machine-readable, stable schema

JSON output is intended for automation, CI, and future tooling.

---

## Doctor Sections & Checks

### 1. Environment Checks

#### 1.1 Operating System

- Verify Windows OS
- Capture:
  - OS version
  - Architecture

Failure:
- EnvironmentError  
  Unsupported operating system

---

#### 1.2 Runtime Availability

- Verify required .NET runtime is available
- Capture:
  - Runtime version

Failure:
- EnvironmentError  
  Required runtime not available

---

#### 1.3 Elevation / Privileges

- Detect whether process is running elevated
- Record:
  - isElevated: true|false

Failure:
- EnvironmentError  
  Administrative privileges required

Note:  
Doctor does not elevate automatically. It reports only.

---

#### 1.4 Required Tooling Presence

Verify required Windows certificate tooling is available:

- certreq.exe
- certutil.exe

For each:
- resolved path
- version (if available)

Failure:
- EnvironmentError  
  Required Windows certificate tooling not found

---

#### 1.5 Crypto Backend Availability

Verify the application can:
- Load certificates
- Parse X.509 structures
- Read and export keys (in-memory)

Failure:
- EnvironmentError  
  Cryptographic backend unavailable

---

### 2. Configuration Validity

#### 2.1 Request Presence

If --request is supplied:
- File must exist
- File must be readable
- File must parse as a CertificateRequest

Failure:
- ConfigurationError  
  Request file missing or invalid

---

#### 2.2 Required Fields

Validate required request fields are present:
- CA target
- Template
- Subject identity (or CSR)
- Mode-specific requirements

Failure:
- ConfigurationError  
  Required request fields missing

---

#### 2.3 Mode Legality

Validate request mode combinations:

Examples:
- signExistingCsr requires csrPath
- csrPath must exist and be readable
- export.keyPem requires exportablePrivateKey = true

Failure:
- ConfigurationError  
  Illegal request configuration

---

#### 2.4 SAN Validation

Validate each SAN entry:
- Recognized type (dns, ip, etc.)
- Proper formatting

Failure:
- ConfigurationError  
  Invalid SAN entry format

---

### 3. Export Destination Readiness

Performed only for requested exports.

For each export target:

Checks:
- Resolve absolute path
- Parent directory exists
- Parent directory is writable
- File exists?
- Would overwrite?
- Is overwrite allowed?

Evidence captured:
- inputPath
- resolvedPath
- parentExists
- writable
- exists
- overwriteAllowed

Failure:
- ExportError  
  Export destination not writable or would overwrite existing file

Doctor must catch export failures before a request runs.

---

### 4. CA Reachability (Non-Issuing)

Doctor may perform non-mutating reachability checks only.

#### 4.1 Name Resolution

- Resolve CA hostname
- Record resolved addresses

Failure:
- ConnectivityError  
  Unable to resolve CA host

---

#### 4.2 Transport Reachability

- Verify CA host is reachable at the transport level
- No certificate issuance
- No state change

Failure:
- ConnectivityError  
  Unable to reach CA service

---

#### 4.3 Authentication Presence (Non-Validating)

- Determine auth mode that would be used
- Confirm required credentials/context exist

Doctor does not attempt enrollment or authentication.

Failure:
- AuthorizationError  
  Required authentication context not available

---

## Doctor Result Summary

At completion, Doctor emits:

- Overall status: PASS or FAIL
- Count of:
  - Passed checks
  - Failed checks
  - Warnings
- Explicit statement:
  - Request is safe to attempt
  - or Request must not proceed

---

## Guarantees

Doctor guarantees:

- No files written
- No certificates issued
- No system configuration changed
- Every failure is categorized
- Every check explains what was checked, what was found, and how to fix it

---

## Authority

Doctor is a gatekeeper, not a suggestion.

Any zlgetcert request execution:
- Must internally satisfy the same checks
- Must not bypass Doctor invariants
- May reuse Doctor logic

If Doctor would fail, request must fail fast.

---

## Design Intent

Doctor exists to eliminate:
- Guessing
- Trial-and-error certificate issuance
- Half-written files
- It worked on that server failures

If users skip Doctor and hit a failure later, that is a bug.
