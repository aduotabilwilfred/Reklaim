"""Test image upload/serving/deletion against a local disposable API/database.
Run: REKLAIM_API_URL=http://localhost:5142 python3 tests/upload_api_smoke.py
For the startup regression, start the API with a fresh content root (no wwwroot).
The unique test account remains; the test post and image are deleted.
"""
import base64
import json
import os
import uuid
from urllib.error import HTTPError
from urllib.request import Request, urlopen

BASE = os.environ.get('REKLAIM_API_URL', 'http://localhost:5141')
PNG = base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=')


def request(method, path, data=None, token=None, content_type='application/json'):
    headers = {'Content-Type': content_type}
    if token:
        headers['Authorization'] = 'Bearer ' + token
    body = json.dumps(data).encode() if isinstance(data, dict) else data
    try:
        response = urlopen(Request(BASE + path, data=body, headers=headers, method=method), timeout=15)
    except HTTPError as error:
        response = error
    return response.status, response.headers, response.read()


def expect(status, result):
    assert result[0] == status, (status, result)
    return result


def multipart(filename, image):
    boundary = uuid.uuid4().hex
    fields = {'Title': 'Upload regression test', 'Description': 'Temporary image', 'LocationFound': 'Library', 'Category': 'Other', 'PostType': 'Found'}
    parts = [f'--{boundary}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode() for k, v in fields.items()]
    parts += [f'--{boundary}\r\nContent-Disposition: form-data; name="Image"; filename="{filename}"\r\nContent-Type: image/png\r\n\r\n'.encode(), image, f'\r\n--{boundary}--\r\n'.encode()]
    return b''.join(parts), 'multipart/form-data; boundary=' + boundary


credentials = {'name': 'Upload test', 'email': 'uploadtest' + uuid.uuid4().hex + '@st.ug.edu.gh', 'password': uuid.uuid4().hex + 'Aa!9'}
expect(200, request('POST', '/api/auth/register', credentials))
token = json.loads(expect(200, request('POST', '/api/auth/login', credentials))[2])['token']
post_id = None
try:
    body, content_type = multipart('test.PNG', PNG)
    post_id = json.loads(expect(201, request('POST', '/api/itemposts', body, token, content_type))[2])['id']
    post = json.loads(expect(200, request('GET', f'/api/itemposts/{post_id}'))[2])
    image_url = post['imageUrl']
    assert image_url.startswith('/uploads/')
    _, headers, image = expect(200, request('GET', image_url))
    assert headers.get_content_type() == 'image/png'
    assert image == PNG, 'Downloaded bytes must match the upload'
    for filename, image in [('test.txt', PNG), ('empty.png', b''), ('large.png', b'x' * (5 * 1024 * 1024 + 1))]:
        body, content_type = multipart(filename, image)
        expect(400, request('POST', '/api/itemposts', body, token, content_type))
    expect(401, request('DELETE', f'/api/itemposts/{post_id}'))
    expect(204, request('DELETE', f'/api/itemposts/{post_id}', token=token))
    post_id = None
    expect(404, request('GET', image_url))
    print('PASS: first upload is publicly served byte-for-byte, invalid uploads rejected, deletion removes image.')
finally:
    if post_id is not None:
        expect(204, request('DELETE', f'/api/itemposts/{post_id}', token=token))
