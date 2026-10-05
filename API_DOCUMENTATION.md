# Reklaim API Documentation

Backend API for the Reklaim campus lost-and-found application.

## Base URL

For local development:

```text
http://localhost:5141
```

For the deployed Render API, replace the placeholder below with the URL shown by Render:

```text
https://your-render-api.onrender.com
```

All examples below use:

```text
https://your-render-api.onrender.com
```

## Authentication

Authentication uses JWT bearer tokens.

1. Register with `POST /api/Auth/register`.
2. Log in with `POST /api/Auth/login`.
3. Copy the returned `token`.
4. Send it on protected requests:

```http
Authorization: Bearer YOUR_JWT_TOKEN
```

Tokens expire after one hour.

## Response conventions

| Status | Meaning |
|---|---|
| `200 OK` | Request completed successfully |
| `201 Created` | A resource was created |
| `204 No Content` | Request completed without a response body |
| `400 Bad Request` | Invalid input or business rule violation |
| `401 Unauthorized` | Authentication is missing or invalid |
| `403 Forbidden` | Authenticated user is not allowed to perform the action |
| `404 Not Found` | Resource does not exist |

## Health

### Check API health

```http
GET /health
```

Authentication: none.

Example response:

```json
{
  "status": "healthy",
  "service": "reklaim-api"
}
```

## Authentication endpoints

### Register

```http
POST /api/Auth/register
Content-Type: application/json
```

Request:

```json
{
  "name": "Test User",
  "email": "student@st.ug.edu.gh",
  "password": "QaTest!12345"
}
```

Registration only accepts `@st.ug.edu.gh` email addresses.

Successful response:

```json
{
  "message": "Registration successful"
}
```

### Login

```http
POST /api/Auth/login
Content-Type: application/json
```

Request:

```json
{
  "email": "student@st.ug.edu.gh",
  "password": "QaTest!12345"
}
```

Successful response:

```json
{
  "token": "YOUR_JWT_TOKEN"
}
```

Invalid credentials return `401 Unauthorized`.

## Item post endpoints

Public item-post endpoints do not require a JWT. Creating, updating, and deleting posts require a JWT.

### List posts

```http
GET /api/ItemPosts
```

Optional query parameters:

| Parameter | Description |
|---|---|
| `type` | `Lost` or `Found` |
| `category` | Exact, case-insensitive category |
| `status` | `Active`, `Claimed`, or `Resolved` |
| `search` | Matches title or description |
| `location` | Matches part of the location |
| `dateFrom` | Start date in `YYYY-MM-DD` format |
| `dateTo` | End date in `YYYY-MM-DD` format |

Example:

```http
GET /api/ItemPosts?type=Found&status=Active&search=wallet&location=Library
```

Successful response:

```json
[
  {
    "id": 1,
    "title": "Blue wallet",
    "description": "Blue leather wallet found near the library",
    "locationFound": "Balme Library",
    "category": "Wallet",
    "postType": "Found",
    "imageUrl": "/uploads/example.png",
    "datePosted": "2026-10-05T10:30:00Z",
    "status": "Active",
    "postedByUserId": 1,
    "postedByName": "Test User"
  }
]
```

### Get one post

```http
GET /api/ItemPosts/{id}
```

Example:

```http
GET /api/ItemPosts/1
```

### Create a post

```http
POST /api/ItemPosts
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: multipart/form-data
```

Form fields:

| Field | Required | Description |
|---|---:|---|
| `Title` | Yes | Item title |
| `Description` | Yes | Item description |
| `LocationFound` | Yes | Where the item was lost or found |
| `Category` | Yes | Item category |
| `PostType` | Yes | `Lost` or `Found` |
| `Image` | No | `.jpg`, `.jpeg`, or `.png`, maximum 5 MB |

Example response:

```json
{
  "id": 1
}
```

### Update post status

```http
PATCH /api/ItemPosts/{id}/status
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

Request:

```json
{
  "status": "Claimed"
}
```

Only the post owner can update its status.

### Delete a post

```http
DELETE /api/ItemPosts/{id}
Authorization: Bearer YOUR_JWT_TOKEN
```

Only the post owner can delete a post. If the post has an image, the image is also deleted.

## Claim endpoints

All claim endpoints require a JWT.

### Submit a claim

```http
POST /api/Claims
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

Request:

```json
{
  "postId": 1,
  "proofDescription": "I can describe the unique sticker inside the item."
}
```

Rules:

- A user cannot claim their own post.
- The post must have `Active` status.
- A user can have at most three pending claims.
- A user can have only one pending claim per post.

Successful response:

```json
{
  "id": 1,
  "message": "Claim submitted. Wait for the finder to review it."
}
```

### Get a claim

```http
GET /api/Claims/{id}
Authorization: Bearer YOUR_JWT_TOKEN
```

Only the claimant or the owner of the related post can view the claim.

### Get my claims

```http
GET /api/Claims/my-claims
Authorization: Bearer YOUR_JWT_TOKEN
```

Returns claims submitted by the authenticated user.

### Get claims on my posts

```http
GET /api/Claims/on-my-posts
Authorization: Bearer YOUR_JWT_TOKEN
```

Returns incoming claims for posts owned by the authenticated user.

### Review a claim

```http
POST /api/Claims/{id}/review
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

Approve:

```json
{
  "approve": true
}
```

Deny:

```json
{
  "approve": false
}
```

Only the owner of the related post can review the claim. An approved claim changes the post status to `Claimed`.

## Uploaded images

Uploaded images are served publicly using the URL returned in an item-post response:

```http
GET /uploads/{filename}
```

Example:

```text
https://your-render-api.onrender.com/uploads/example.png
```

## Endpoint summary

```text
GET    /health

POST   /api/Auth/register
POST   /api/Auth/login

GET    /api/ItemPosts
GET    /api/ItemPosts/{id}
POST   /api/ItemPosts
PATCH  /api/ItemPosts/{id}/status
DELETE /api/ItemPosts/{id}

POST   /api/Claims
GET    /api/Claims/{id}
GET    /api/Claims/my-claims
GET    /api/Claims/on-my-posts
POST   /api/Claims/{id}/review

GET    /uploads/{filename}
```
