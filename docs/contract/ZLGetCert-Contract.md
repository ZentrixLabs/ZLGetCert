# ZLGetCert Contract  
**Structure Standard – v1**

## Purpose

ZLGetCert is a **deterministic Windows certificate request tool** for issuing certificates from an on-prem Certificate Authority (ADCS), with optional, explicit exports.

Under the BakBeatLabs philosophy, ZLGetCert must be:

- Self-describing (can explain what it will do before it does it)
- Reproducible (same input → same behavior)
- Verifiable (can prove what succeeded and what failed)
- Non-guessing (paths, environment, and intent must be explicit)

This document defines the **authoritative contract** between input and outcome.

---

## Contract Types

### CertificateRequest (Input)

#### Identity / Subject

- **commonName (CN)**  
  Required unless `csrPath` is provided.  
  Example: `example.internal`

- **subjectDn** (optional)  
  Full X.500 distinguished name override.  
  If present, must be a complete DN string.

- **subjectAlternativeNames (SANs)** (optional)  
  List of SAN entries. Each entry must be explicitly typed:
  - `dns:<name>`
  - `ip:<address>`
  - `uri:<uri>` (if supported)

- **wildcard** (boolean)  
  Indicates wildcard intent.  
  Validity is enforced against CA template rules.

---

#### Request Mode

- **mode**
  - `newKeypair` – generate keypair during request
  - `signExistingCsr` – submit an existing CSR

- **csrPath**  
  Required if `mode = signExistingCsr`.  
  Must be an explicit, readable file path.

---

#### Certificate Authority Target

- **caConfig**
  - `caServer` – hostname or FQDN
  - `caName` – CA display name  
  **OR**
  - `configString` – explicit `server\CAName` form

  One and only one CA target must be specified.  
  ZLGetCert must never infer or discover CA configuration.

- **template**  
  Certificate template name as defined on the CA.

---

#### Key / Crypto Profile

- **keyAlgorithm**  
  Example: `RSA`

- **keySize**  
  Example: `2048`, `4096`

- **hashAlgorithm**  
  Example: `sha256`, `sha384`, `sha512`

- **exportablePrivateKey** (boolean)  
  Must be explicitly set to `true` to allow private key export.  
  Default is `false`.

---

#### Authentication Mode

- **authMode**
  - `currentUser` (default) – uses the current Windows security context

ZLGetCert does not prompt for credentials interactively.  
All authentication behavior must be explicit and explainable.

---

#### Export Options (Explicit Paths Only)

Each export is opt-in and path-explicit.

- **exports.leafPem**
  - `enabled` (boolean)
  - `path` (string)

- **exports.keyPem**
  - `enabled` (boolean)
  - `path` (string)

- **exports.caBundlePem**
  - `enabled` (boolean)
  - `path` (string)

- **overwrite** (boolean)  
  Default: `false`  
  Existing files must never be overwritten unless explicitly allowed.

---

#### Operational Metadata

- **requestId** (optional)  
  Caller-supplied correlation identifier.

- **outputFormat**
  - `text`
  - `json`

---

## CertificateResult (Output)

### Core Outcome

- **status**
  - `success`
  - `failed`

- **failureCategory**  
  Present only if `status = failed`.

- **message**  
  Human-readable summary of outcome.

- **requestId**  
  Echoed from request if provided.

- **timestampUtc**

---

### Certificate Details (on success)

- **thumbprint**
- **fingerprints**
  - `sha256`
- **subject**
- **subjectAlternativeNames**
- **issuer**
- **serialNumber**
- **notBefore**
- **notAfter**
- **keyAlgorithm**
- **keySize**

---

### Artifacts

#### Native Returned Artifacts

- `cerPath` (if produced)
- `pfxPath` (if produced)
- `chainPath` (if produced)

#### Exported Artifacts

For each requested export:

- `path`
- `written` (boolean)
- `sizeBytes`
- `sha256`
- Additional metadata as applicable:
  - CA bundle: `certificateCount`

---

### Invariant Proofs

The result must include an invariant report:

Each invariant contains:
- `name`
- `ok` (boolean)
- `detail`

Examples:
- `ExportPathsWritable`
- `NoOverwriteWithoutFlag`
- `LeafPemParses`
- `PrivateKeyPemParses`
- `CaBundleContainsCertificates`

---

## Invariants (Non-Negotiable)

1. **No path guessing**  
   All export destinations must be explicitly provided.

2. **Preflight enforcement**  
   If export is requested, destination validity must be verified *before* certificate request.

3. **No overwrite by default**  
   Existing files are never replaced unless `overwrite = true`.

4. **Private key export is opt-in only**  
   Must be explicitly requested and clearly logged.

5. **Export validity**
   - Certificate PEM must parse as X.509.
   - Key PEM must parse as the expected key type.
   - CA bundle PEM must contain at least one valid CA certificate.

6. **Self-describing result**  
   The output must clearly state what was produced and where it was written.

---

## Failure Categories (Stable)

All failures must map to exactly one category:

- **ConfigurationError**  
  Missing inputs, invalid combinations, malformed SANs.

- **EnvironmentError**  
  Missing OS capability, missing tooling, insufficient privileges.

- **ConnectivityError**  
  CA host unreachable, DNS failure, transport failure.

- **AuthorizationError**  
  Access denied, enrollment permission failure.

- **CARequestError**  
  Request rejected, template mismatch, policy denial.

- **ExportError**  
  Invalid path, permission failure, overwrite violation, write failure.

- **FormatError**  
  Returned certificate or key cannot be parsed or converted.

---

## Contract Authority

This document is the **authoritative behavioral definition** of ZLGetCert.

Any future feature, UI, or automation:
- Must not violate these invariants
- Must map behavior to this contract
- Must surface failures using these categories

If behavior is ambiguous, this contract wins.

