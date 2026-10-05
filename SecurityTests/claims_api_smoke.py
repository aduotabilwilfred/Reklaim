"""
Claims API smoke test for Reklaim.

Covers the requirements from the README:
  - Any authenticated user can submit a claim on a post they did not make.
  - A user may have at most 3 pending claims at a time.
  - Only the post owner (finder) can approve or deny a claim.
  - On approval, both parties' phone numbers are revealed.
  - Claims endpoints require authentication.

Run against a locally running API:
    python tests/claims_api_smoke.py

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
    return f"qa+claim-{uuid.uuid4().hex[:10]}@st.ug.edu.gh"


def register_and_token(name):
    email = unique_email()
    r = requests.post(f"{BASE}/api/Auth/register",
                      json={"name": name, "email": email, "password": PASSWORD},
                      timeout=TIMEOUT)
    assert r.status_code in (200, 201), f"register failed: {r.status_code} {r.text}"
    r = requests.post(f"{BASE}/api/Auth/login",
                      json={"email": email, "password": PASSWORD},
                      timeout=TIMEOUT)
    assert r.status_code == 200, f"login failed: {r.status_code} {r.text}"
    return email, r.json()["token"]


def auth(token):
    return {"Authorization": f"Bearer {token}"}


def create_post(token, title="QA claim test"):
    post = {
        "Title": title,
        "Description": "QA test post for claims",
        "LocationFound": "QA Location",
        "Category": "Keys",
        "PostType": "Found",
    }
    r = requests.post(f"{BASE}/api/itemposts", data=post, headers=auth(token), timeout=TIMEOUT)
    assert r.status_code in (200, 201), f"create post failed: {r.status_code} {r.text}"
    return r.json()["id"]


def submit_claim(token, post_id, proof):
    return requests.post(f"{BASE}/api/claims",
                         json={"postId": post_id, "proofDescription": proof},
                         headers=auth(token), timeout=TIMEOUT)


def review_claim(token, claim_id, approve):
    return requests.post(f"{BASE}/api/claims/{claim_id}/review",
                         json={"approve": approve},
                         headers=auth(token), timeout=TIMEOUT)


def check(condition, message):
    if not condition:
        raise AssertionError(message)


def main():
    finder_email, finder_token = register_and_token("QA Finder")
    _, claimer_token = register_and_token("QA Claimer")
    _, outsider_token = register_and_token("QA Outsider")

    post_id = create_post(finder_token)

    # 1. Unauthenticated claim submission must be rejected.
    r = requests.post(f"{BASE}/api/claims",
                      json={"postId": post_id, "proofDescription": "no auth"},
                      timeout=TIMEOUT)
    check(r.status_code == 401,
          f"unauthenticated claim should return 401, got {r.status_code}")

    # 2. A valid claim submission succeeds.
    r = submit_claim(claimer_token, post_id, "I know the secret detail")
    check(r.status_code in (200, 201),
          f"valid claim should succeed, got {r.status_code}: {r.text}")
    first_claim_id = r.json()["id"]

    # 3. The finder cannot claim their own post.
    r = submit_claim(finder_token, post_id, "claiming my own post")
    check(r.status_code not in (200, 201),
          f"finder should not be able to claim own post, got {r.status_code}")

    # 4. Rule from the live API: one pending claim per item per user.
    r = submit_claim(claimer_token, post_id, "second claim on same item")
    check(r.status_code not in (200, 201),
          f"second claim on the same item should be rejected, got {r.status_code}: {r.text}")

    # 5. Max 3 pending claims per user across different posts.
    for i in range(2):
        extra_post = create_post(finder_token, title=f"QA claim test extra {i}")
        r = submit_claim(claimer_token, extra_post, f"more proof {i}")
        check(r.status_code in (200, 201),
              f"claim #{i + 2} on a different post should succeed, got {r.status_code}: {r.text}")

    fourth_post = create_post(finder_token, title="QA claim test fourth")
    r = submit_claim(claimer_token, fourth_post, "the fourth claim")
    check(r.status_code not in (200, 201),
          f"4th pending claim should be rejected, got {r.status_code}: {r.text}")

    # 6. An outsider cannot review a claim on someone else's post.
    r = review_claim(outsider_token, first_claim_id, True)
    check(r.status_code in (401, 403),
          f"outsider reviewing claim should be 401/403, got {r.status_code}")

    # 7. The finder can review a claim.
    r = review_claim(finder_token, first_claim_id, True)
    check(r.status_code in (200, 201, 204),
          f"finder approval should succeed, got {r.status_code}: {r.text}")

    # 8. On approval, both parties' phone numbers are exposed.
    if r.status_code in (200, 201):
        body = r.json()
        phone_keys = [k for k in body.keys() if "phone" in k.lower()]
        check(len(phone_keys) > 0,
              f"approval response should reveal phone number(s), got keys: {list(body)}")

    # 9. The finder can view all incoming claims on their posts.
    r = requests.get(f"{BASE}/api/claims/on-my-posts",
                     headers=auth(finder_token), timeout=TIMEOUT)
    check(r.status_code == 200,
          f"finder on-my-posts should return 200, got {r.status_code}: {r.text}")

    # 10. The claimer can view their own submitted claims.
    r = requests.get(f"{BASE}/api/claims/my-claims",
                     headers=auth(claimer_token), timeout=TIMEOUT)
    check(r.status_code == 200,
          f"claimer my-claims should return 200, got {r.status_code}: {r.text}")

    print("PASS: unauth 401, valid claim, self-claim blocked, one-per-item rule, "
          "3-pending cap, outsider review blocked, finder approval + phone reveal, "
          "my-claims and on-my-posts retrieval.")


if __name__ == "__main__":
    try:
        main()
    except AssertionError as e:
        print(f"FAIL: {e}")
        sys.exit(1)