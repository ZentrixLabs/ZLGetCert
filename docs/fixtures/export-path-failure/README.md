# Export Path Failure Fixture

## Intent

This fixture proves ZLGetCert never writes blindly. It validates path validation, overwrite protection, and export error categorization. The fixture uses a protected system path that will fail write operations, triggering an ExportError.

## Note: CA transport check is intentionally non-guessy

Doctor includes a `ca.transport` check.

- If `ca.caConfig.port` is NOT set in the request, `ca.transport` will show **WARN** and will not attempt a TCP reachability test.
- This is intentional: ADCS enrollment paths vary, and ZLGetCert refuses to guess ports or channels.

If you want transport verification, set an explicit port in the request JSON:

- `ca.caConfig.port: 443` (only if your environment actually uses a CA HTTPS endpoint)
- otherwise set the correct port for your known enrollment path

DNS resolution (`ca.dns`) remains required and will FAIL if the CA host cannot be resolved.

## Expected Outcomes

### Doctor Command
```
zlgetcert doctor --request docs/fixtures/export-path-failure/request.json
```

**Expected Result:**
- Exit code: 2
- Failure category: "ExportError"
- Evidence includes:
  - Resolved absolute path
  - Exists / writable / overwrite analysis
  - Clear explanation of why the export path will fail

### Request Command
```
zlgetcert request --request docs/fixtures/export-path-failure/request.json --format json
```

**Expected Result:**
- **Preferred:** Doctor blocks request from proceeding
- **Acceptable:** Request succeeds but export fails with ExportError
- **Never acceptable:** Silent overwrite or partial write without error

## Notes

- This fixture enforces the "no surprises" rule
- Export safety is non-negotiable
- The protected system path (C:\Windows\System32\leaf.pem) with overwrite=false ensures this will fail in most environments
- Even if the file doesn't exist, System32 is typically protected and requires elevation to write

