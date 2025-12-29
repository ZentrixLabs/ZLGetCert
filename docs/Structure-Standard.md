# ZLGetCert Structure Standard (BakBeatLabs)

This folder contains the minimal "Structure Standard" for refactoring ZLGetCert into a self-describing, reproducible tool.

## Anchors

1) Contract (source of truth)
- docs/contract/ZLGetCert-Contract.md

2) Doctor (read-only preflight)
- docs/doctor/Doctor-Spec.md

3) CLI (reference behavior)
- docs/cli/CLI-Reference.md

4) Fixtures (proof artifacts)
- docs/fixtures/Fixtures.md

## Rule

If behavior is ambiguous:
- Contract wins
- Doctor blocks unsafe runs
- CLI is the canonical interface
- Fixtures are proof

Nothing else needs to be built until these remain stable.

