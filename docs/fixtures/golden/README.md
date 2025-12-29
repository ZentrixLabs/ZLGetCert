# Golden Path Fixture

## Intent

This fixture demonstrates the happy path where:
- Doctor passes all checks
- Certificate request succeeds
- All requested exports are written successfully
- Outputs are valid and parseable

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
zlgetcert doctor --request docs/fixtures/golden/request.json
```

**Expected Result:**
- Exit code: 0
- No failed checks
- All environment and path validations pass

### Request Command
```
zlgetcert request --request docs/fixtures/golden/request.json --format json
```

**Expected Result:**
- Exit code: 0
- status = "success"
- All requested exports written to specified paths
- Invariants verified:
  - Export paths writable
  - No overwrite violations
  - Leaf PEM parses as valid X.509 certificate
  - CA bundle contains ≥ 1 certificate
  - Private key exported only if explicitly requested

## Notes

- Intended for a known lab environment with a working CA
- Uses a safe, short-lived test template
- Export paths should point to a disposable directory
- This fixture should pass in the intended test environment

