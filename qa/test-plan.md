\# Test Plan — Reklaim



\## 1. Purpose



Describe the testing approach for the Reklaim Lost \& Found API, covering the

scope of what will be tested, how, and what "done" means for QA.



\## 2. Scope



\### In scope



\- \*\*Auth\*\*: registration, login, JWT issuance, token-based access control, domain restriction

\- \*\*Item Posts (Hub)\*\*: browsing, filtering, searching, creating, updating status, deleting

\- \*\*Uploads\*\*: image upload, public retrieval, format/size validation, deletion

\- \*\*Claims\*\*: submitting, the 3-pending-per-user cap, one-per-item rule, approval/denial, phone reveal



\### Out of scope



\- Frontend (tested by the frontend team)

\- Hosting, deployment, and CI/CD

\- Load, stress, and penetration testing

\- Email confirmation flows (not present in the API)



\## 3. Test Approach



\- \*\*Type\*\*: API smoke / regression testing (black-box, HTTP-level)

\- \*\*Tool\*\*: Python 3.12 with `requests`, scripted tests under `tests/`

\- \*\*Target\*\*: a running instance of the API (default `http://localhost:5141`)

\- \*\*Method\*\*: each script performs a full scenario end-to-end, asserts expected

&#x20; responses, and cleans up after itself



\## 4. Test Environment



| Item | Value |

|---|---|

| API | ASP.NET Core Web API, .NET 10 |

| Database | PostgreSQL 18 (local) |

| Runtime | Python 3.12, `requests` library |

| Config override | `REKLAIM\_API\_URL` environment variable |



\## 5. Test Files



| File | Coverage |

|---|---|

| `tests/auth\_api\_smoke.py` | Registration, login, JWT, protected endpoints |

| `tests/hub\_api\_smoke.py` | Post browsing, filters, validation, status updates |

| `tests/upload\_api\_smoke.py` | Image upload, retrieval, validation, deletion |

| `tests/claims\_api\_smoke.py` | Claim submission, caps, review, phone reveal |



\## 6. Entry Criteria



\- Backend API builds and starts

\- Database is reachable and migrated

\- Test environment URL is available



\## 7. Exit Criteria



\- All smoke tests pass

\- Every requirement in `qa/traceability-matrix.md` is covered

\- Any documentation gaps or bugs are reported



\## 8. How to Run



```bash

cd Reklaim

python -u tests/auth\_api\_smoke.py

python -u tests/hub\_api\_smoke.py

python -u tests/upload\_api\_smoke.py

python -u tests/claims\_api\_smoke.py

