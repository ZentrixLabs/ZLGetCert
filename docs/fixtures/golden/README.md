# Golden Path Fixture

## Intent

This fixture demonstrates the happy path where:
- Doctor passes all checks
- Certificate request succeeds
- All requested exports are written successfully
- Outputs are valid and parseable

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

