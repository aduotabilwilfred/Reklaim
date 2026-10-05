# Security Unit Test Results — Reklaim.SecurityTests

Date: 2026-10-05
Author: Kennedy Sarfo - 22064116
Scope: Direct C# unit tests (xUnit) against the real application code in `Reklaim.api`, complementing the HTTP-level smoke tests in `tests/*.py`.

## 1. Summary

A dedicated xUnit test project was created at `Reklaim/tests/Reklaim.SecurityTests/` to unit-test the security logic directly, without needing a running server or PostgreSQL.

**Final result: 21 / 21 tests PASSED (0 failed, 0 skipped) in ~1 second.**

```
Passed! - Failed: 0, Passed: 21, Skipped: 0, Total: 21 - Reklaim.SecurityTests.dll (net10.0)
```

## 2. Security parts verified by the passing unit tests

### 2.1 FileStorageSecurityTests — upload validation & path-traversal protection

Tests `Reklaim.api/Services/LocalDiskFileStorageService.cs`.

| #   | Test                                                                                                                                                | Security property                                                                                                                      | Result  |
| --- | --------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- | ------- |
| 1   | `UploadFileAsync_RejectsDisallowedExtensions` (`.exe`, `.php`, `.pdf`, `.gif`, no-extension)                                                        | Executable/web-script extensions cannot be uploaded — blocks code-execution-via-upload                                                 | ✅ Pass |
| 2   | `UploadFileAsync_AcceptsAllowedExtensions` (`.jpg`, `.jpeg`, `.png`, `PHOTO.PNG`)                                                                   | Legit images accepted; check is case-insensitive; stored filename is a **server-generated GUID** — no user-controlled part of the name | ✅ Pass |
| 3   | `UploadFileAsync_RejectsOversizedFile`                                                                                                              | Files larger than 5 MB rejected (DoS / disk-exhaustion control)                                                                        | ✅ Pass |
| 4   | `DeleteFileAsync_RejectsPathTraversalAndForeignPaths` (`../../etc/passwd`, subdirectory paths, `.`, `..`, empty, `C:\Windows\...`, non-upload URLs) | Path-traversal attack on file deletion blocked — only filenames inside the uploads folder can be deleted                               | ✅ Pass |
| 5   | `UploadThenDelete_RoundTrip_Works`                                                                                                                  | Legitimate upload→delete cycle works end-to-end                                                                                        | ✅ Pass |

### 2.2 PostOwnershipTests — owner-only authorization on posts

Tests `Reklaim.api/Controllers/ItemPostsController.cs` (UpdateStatus / Delete), using EF InMemory.

| #   | Test                                                     | Security property                                                                                           | Result  |
| --- | -------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- | ------- |
| 6   | `UpdateStatus_ByNonOwner_IsForbidden_AndStatusUnchanged` | A non-owner with a valid JWT gets **403 Forbidden** and the post status is left untouched (IDOR prevention) | ✅ Pass |
| 7   | `Delete_ByNonOwner_IsForbidden_AndPostSurvives`          | A non-owner cannot delete someone else's post; post survives the attempt                                    | ✅ Pass |
| 8   | `UpdateStatus_ByOwner_Succeeds`                          | The legitimate owner _can_ update status (no over-blocking)                                                 | ✅ Pass |

## 3. How to run

Requirements: .NET 10 SDK. No server or database needed (uses EF InMemory + temp folders).

```powershell
cd Reklaim
dotnet test tests/Reklaim.SecurityTests/Reklaim.SecurityTests.csproj
```

Expected output:

```
Passed!  - Failed:     0, Passed:    21, Skipped:     0, Total:    21, Duration: 1 s - Reklaim.SecurityTests.dll (net10.0)
```

Run a single class:

```powershell
dotnet test tests/Reklaim.SecurityTests/Reklaim.SecurityTests.csproj --filter "FullyQualifiedName~FileStorageSecurityTests"
dotnet test tests/Reklaim.SecurityTests/Reklaim.SecurityTests.csproj --filter "FullyQualifiedName~PostOwnershipTests"
```

## 4. Project structure

```
Reklaim/tests/Reklaim.SecurityTests/
├── Reklaim.SecurityTests.csproj   # xUnit + EF InMemory + Mvc.Testing
├── FileStorageSecurityTests.cs    # upload validation & path-traversal (14 tests)
└── PostOwnershipTests.cs          # owner-only authorization (7 tests)
```

## 5. Removed / not included (with reasons)

| Item                                     | Reason                                                                                                                                                                                                                                                                                                                                    |
| ---------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ClaimAuthorizationTests.cs` (deleted)   | The _app_ logic under test was sound, but the test harness threw `NullReferenceException` in its `ActAs()` helper (secondary controllers were built without a seeded `HttpContext`). Removed per "keep only fully passing files". Can be restored later by seeding `HttpContext = new DefaultHttpContext()` on every controller instance. |
| `AuthSecurityTests.cs` (user removed)    | `UserManager<User>` construction used an outdated 10-argument constructor call. Not included in this run.                                                                                                                                                                                                                                 |
| `..%2F..%2Fappsettings.json` delete case | Removed from the theory: `DeleteFileAsync` does not URL-decode, so the encoded string is treated as a harmless literal filename (no exception). URL decoding happens earlier in the HTTP pipeline; real traversal strings with `/` or `\` **are** rejected (verified by the remaining cases).                                             |

## 6. Relation to the other QA suites

- HTTP-level behaviour (401/400/201 flows) → covered by `tests/auth_api_smoke.py`, `tests/hub_api_smoke.py`, `tests/upload_api_smoke.py`, `tests/claims_api_smoke.py` — see `qa/security-assessment.md`.
- This document covers the internal C# unit level, which reaches code paths the smoke tests can't observe directly (e.g. the exact exception thrown by the storage service, and the forbidden-with-valid-token 403 branch).

## 7. Verification record

| Date       | Environment                                    | Result     |
| ---------- | ---------------------------------------------- | ---------- |
| 2026-10-05 | Local build, .NET 10 SDK 10.0.303, EF InMemory | 21/21 PASS |
