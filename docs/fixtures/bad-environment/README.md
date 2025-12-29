# Bad Environment Fixture

## Intent

This fixture proves Doctor's ability to detect environment failures early, before any certificate request is attempted. The fixture uses an invalid CA hostname that will not resolve, triggering a ConnectivityError or EnvironmentError.

## Expected Outcomes

### Doctor Command
```
zlgetcert doctor --request docs/fixtures/bad-environment/request.json
```

**Expected Result:**
- Exit code: 2
- At least one failed check
- failureCategory: "EnvironmentError" or "ConnectivityError"
- Clear remediation instructions explaining why it failed and how to fix it

### Request Command
```
zlgetcert request --request docs/fixtures/bad-environment/request.json --format json
```

**Expected Result:**
- Request must not be attempted (Doctor should block it)
- If attempted, must fail fast with the same failure category as Doctor
- Never should proceed to actual certificate request

## Notes

- This fixture exists to prevent "just try it and see" behavior
- Output should clearly explain why it failed (CA hostname does not resolve) and how to fix it
- The invalid CA hostname ensures this fixture will fail in most environments

