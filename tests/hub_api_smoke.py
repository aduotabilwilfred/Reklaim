"""Run against a local API with a disposable database: python3 tests/hub_api_smoke.py.
Creates a unique test account and deletes its item posts after testing.
"""
import json
import os
import uuid
from datetime import datetime, timedelta, timezone
from urllib.error import HTTPError
from urllib.parse import urlencode
from urllib.request import Request, urlopen

BASE = os.environ.get('REKLAIM_API_URL', 'http://localhost:5141')
TOKEN = None


def call(method, path, data=None, form=False):
    headers = {}
    if TOKEN:
        headers['Authorization'] = 'Bearer ' + TOKEN
    if form:
        boundary = uuid.uuid4().hex
        body = ''.join(f'--{boundary}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n' for k, v in data.items())
        body = (body + f'--{boundary}--\r\n').encode()
        headers['Content-Type'] = 'multipart/form-data; boundary=' + boundary
    elif data is not None:
        body = json.dumps(data).encode()
        headers['Content-Type'] = 'application/json'
    else:
        body = None
    try:
        response = urlopen(Request(BASE + path, data=body, headers=headers, method=method), timeout=15)
    except HTTPError as error:
        response = error
    raw = response.read()
    return response.status, json.loads(raw) if raw else None


def expect(method, path, status, data=None, form=False):
    actual, result = call(method, path, data, form)
    assert actual == status, (method, path, actual, result)
    return result


marker = 'hubtest' + uuid.uuid4().hex
credentials = {'name': 'Hub API test', 'email': marker + '@st.ug.edu.gh', 'password': 'HubTest!29aX'}
expect('POST', '/api/auth/register', 200, credentials)
TOKEN = expect('POST', '/api/auth/login', 200, credentials)['token']
ids = []
try:
    today = datetime.now(timezone.utc).date()
    post = dict(Title=marker + ' Keys', Description='Blue keyring', LocationFound='Balme Library', Category='Keys', PostType='Found')
    for changes in ({}, {'Title': marker + ' Book', 'LocationFound': 'Commonwealth Hall', 'Category': 'Books', 'PostType': 'Lost'}):
        result = expect('POST', '/api/itemposts', 201, post | changes, True)
        ids.append(result['id'])

    def feed(**filters):
        return expect('GET', '/api/itemposts?' + urlencode({'search': marker.upper()} | filters), 200)

    assert len(feed()) == 2
    assert [p['id'] for p in feed(location='  BALME  ', type='Found', category=' keys ', status='Active', dateFrom=str(today), dateTo=str(today))] == [ids[0]]
    assert len(feed(dateTo=str(today - timedelta(days=1)))) == 0
    assert len(feed(dateFrom=str(today + timedelta(days=1)))) == 0
    assert len(feed(dateTo='9999-12-31')) == 2
    assert len(feed(location='does-not-exist')) == 0
    assert len(feed(search='BLUE KEYRING', location='Balme')) >= 1
    for query in ('type=999', 'type=unknown', 'status=-1', 'dateFrom=bad', 'dateFrom=2026-09-29&dateTo=2026-09-28'):
        expect('GET', '/api/itemposts?' + query, 400)
    for changes in ({'Title': ' '}, {'Description': ''}, {'LocationFound': ' '}, {'Category': ''}, {'PostType': '99'}):
        expect('POST', '/api/itemposts', 400, post | changes, True)
    for body in ({}, {'status': 99}):
        expect('PATCH', f'/api/itemposts/{ids[0]}/status', 400, body)
    expect('PATCH', f'/api/itemposts/{ids[0]}/status', 204, {'status': 2})
    assert len(feed(status='Resolved')) == 1
    print('PASS: combined filters, UTC dates, case-insensitive search, invalid inputs, and status updates.')
finally:
    for post_id in ids:
        expect('DELETE', f'/api/itemposts/{post_id}', 204)
