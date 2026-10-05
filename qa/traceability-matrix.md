\# Traceability Matrix



Maps every requirement from the README to the test that verifies it.



| # | Requirement (from README) | Source | Test file | Status |

|---|---|---|---|---|

| 1 | Registration limited to @st.ug.edu.gh emails | README, "Domain Restriction" | `tests/auth\_api\_smoke.py` | ✅ Pass |

| 2 | Valid @st.ug.edu.gh registration succeeds | README, Auth table | `tests/auth\_api\_smoke.py` | ✅ Pass |

| 3 | Duplicate registration is rejected | README, Auth table (implied) | `tests/auth\_api\_smoke.py` | ✅ Pass |

| 4 | Login returns a 1-hour JWT | README, Auth table | `tests/auth\_api\_smoke.py` | ✅ Pass |

| 5 | Login with wrong password is rejected | README, Auth table | `tests/auth\_api\_smoke.py` | ✅ Pass |

| 6 | Protected endpoints reject missing token | README, Claims table | `tests/auth\_api\_smoke.py` | ✅ Pass |

| 7 | Protected endpoints accept valid token | README, Claims table | `tests/auth\_api\_smoke.py` | ✅ Pass |

| 8 | Browse all posts (GET /api/itemposts) | README, Item Posts table | `tests/hub\_api\_smoke.py` | ✅ Pass |

| 9 | Filter by type, category, status | README, "Hub filters" | `tests/hub\_api\_smoke.py` | ✅ Pass |

| 10 | Search matches part of title/description (case-insensitive) | README, "Hub filters" | `tests/hub\_api\_smoke.py` | ✅ Pass |

| 11 | Location matches part of LocationFound (case-insensitive) | README, "Hub filters" | `tests/hub\_api\_smoke.py` | ✅ Pass |

| 12 | dateFrom/dateTo use YYYY-MM-DD, inclusive UTC days | README, "Hub filters" | `tests/hub\_api\_smoke.py` | ✅ Pass |

| 13 | Invalid dates / reversed ranges return 400 | README, "Hub filters" | `tests/hub\_api\_smoke.py` | ✅ Pass |

| 14 | Undefined type/status values return 400 | README, "Hub filters" | `tests/hub\_api\_smoke.py` | ✅ Pass |

| 15 | Creating a post requires nonblank title, description, location, category | README, "Hub filters" | `tests/hub\_api\_smoke.py` | ✅ Pass |

| 16 | Status update requires a defined status value | README, "Hub filters" | `tests/hub\_api\_smoke.py` | ✅ Pass |

| 17 | Uploaded images are served publicly at /uploads/<filename> | README, "Image delivery" | `tests/upload\_api\_smoke.py` | ✅ Pass |

| 18 | Only JPG, JPEG, PNG accepted (up to 5 MB) | README, "Image delivery" | `tests/upload\_api\_smoke.py` | ✅ Pass |

| 19 | Deleting a post removes its local image | README, "Image delivery" | `tests/upload\_api\_smoke.py` | ✅ Pass |

| 20 | Max 3 pending claims per user | README, Claims table | `tests/claims\_api\_smoke.py` | ✅ Pass |

| 21 | Claim submission requires authentication | README, Claims table | `tests/claims\_api\_smoke.py` | ✅ Pass |

| 22 | Finder can approve or deny a claim | README, Claims table | `tests/claims\_api\_smoke.py` | ✅ Pass |

| 23 | Only the finder can review a claim | README, Claims table | `tests/claims\_api\_smoke.py` | ✅ Pass |

| 24 | On approval, both parties' phone numbers are revealed | README, Claims table | `tests/claims\_api\_smoke.py` | ✅ Pass |

| 25 | Claimer can view their submitted claims (GET /api/claims/my-claims) | README, Claims table | `tests/claims\_api\_smoke.py` | ✅ Pass |

| 26 | Finder can view incoming claims (GET /api/claims/on-my-posts) | README, Claims table | `tests/claims\_api\_smoke.py` | ✅ Pass |



\## Documented gaps (behaviour that is real but not in the README)



| # | Behaviour observed | Evidence | Recommendation |

|---|---|---|---|

| G1 | Passwords must contain at least one non-alphanumeric character | `400: {"errors":\["Passwords must have at least one non alphanumeric character."]}` | Document in README so the frontend can show the rule |

| G2 | A user may have only one pending claim per item | `400: You already have a pending claim on this item.` | Document in README alongside the 3-pending-per-user rule |



\## Notes



\- All tests run against a live API (default `http://localhost:5141`).

\- Override the target with `REKLAIM\_API\_URL` to test a hosted environment.

\- Smoke tests generate unique test accounts and clean up after themselves.

