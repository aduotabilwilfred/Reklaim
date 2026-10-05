"""
Auth API smoke test for Reklaim.

Covers the requirements from the README:
  - Registration is limited to @st.ug.edu.gh email addresses only.
  - Login returns a JWT.
  - Protected endpoints reject requests without a token.

Run against a locally running API:
    python tests/auth_api_smoke.py

Override the API URL with the REKLAIM_API_URL environment variable.
"""
import os
import sys
import uuid

import requests

BASE = os.environ.get("REKLAIM_API_URL", "http://localhost:5141").rstrip("/")
TIMEOUT = 10
PASSWORD = "QaTest!12345"


def unique_email():
    return f"qa+auth-{uuid.uuid4().hex[:10]}@st.ug.edu.gh"


def post(path, json=None, headers=None):
    return requests.post(f"{BASE}{path}", json=json, headers=headers, timeout=TIMEOUT)


def get(path, headers=None):
    return requests.get(f"{BASE}{path}", headers=headers, timeout=TIMEOUT)


def register(email, name="QA Test", password=PASSWORD):
    return post("/api/Auth/register",
                {"name": name, "email": email, "password": password})


def login(email, password=PASSWORD):
    return post("/api/Auth/login", {"email": email, "password": password})


def check(condition, message):
    if not condition:
        raise AssertionError(message)


def main():
    # 1. Non-@st.ug.edu.gh emails must be rejected.
    r = register(f"qa+auth-{uuid.uuid4().hex[:10]}@gmail.com")
    check(r.status_code not in (200, 201),
          f"non-@st.ug.edu.gh email should be rejected, got {r.status_code}")

    # 2. Valid @st.ug.edu.gh email registers successfully.
    email = unique_email()
    r = register(email)
    check(r.status_code in (200, 201),
          f"valid registration should succeed, got {r.status_code}: {r.text}")

    # 3. Duplicate registration is rejected.
    r = register(email)
    check(r.status_code not in (200, 201),
          f"duplicate registration should be rejected, got {r.status_code}")

    # 4. Login with correct password returns a non-empty token.
    r = login(email)
    check(r.status_code == 200,
          f"login with correct password should return 200, got {r.status_code}: {r.text}")
    body = r.json()
    check("token" in body and isinstance(body["token"], str) and body["token"],
          f"login response should contain a non-empty 'token' field, got keys: {list(body)}")
    token = body["token"]

    # 5. Login with wrong password returns 401.
    r = login(email, password="WrongPassword999")
    check(r.status_code == 401,
          f"login with wrong password should return 401, got {r.status_code}")

    # 6. Protected endpoint rejects a request with no token.
    r = get("/api/claims/my-claims")
    check(r.status_code == 401,
          f"protected endpoint without token should return 401, got {r.status_code}")

    # 7. Protected endpoint accepts a request with a valid token.
    r = get("/api/claims/my-claims", headers={"Authorization": f"Bearer {token}"})
    check(r.status_code == 200,
          f"protected endpoint with valid token should return 200, got {r.status_code}: {r.text}")

    print("PASS: domain restriction, duplicate rejection, JWT issuance, "
          "wrong-password 401, protected endpoint auth.")


if __name__ == "__main__":
    try:
        main()
    except AssertionError as e:
        print(f"FAIL: {e}")
        sys.exit(1)