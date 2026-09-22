# Reklaim — Campus Lost & Found

A web platform that helps university students recover lost items by connecting **finders** and **owners** through a secure, accountable system — restricted to verified university email addresses.

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Team & Roles](#team--roles)
- [Getting Started (Backend)](#getting-started-backend)
- [Environment Variables & Secrets](#environment-variables--secrets)
- [API Endpoints](#api-endpoints)
- [Git Workflow](#git-workflow)

---

## Overview

Reklaim allows students to:
- **Post** a found item with a photo, description, and location.
- **Browse** the Hub — a searchable, filterable feed of Lost and Found items.
- **Claim** an item by submitting a proof description (e.g., a detail only the true owner would know).
- Have the finder **approve or deny** the claim. On approval, both parties' **phone numbers are privately revealed** so they can coordinate the handoff directly.

> **Domain Restriction:** Registration is limited to `@st.ug.edu.gh` email addresses only.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend API | ASP.NET Core Web API (.NET 10) |
| Authentication | ASP.NET Core Identity + JWT Bearer |
| Database ORM | Entity Framework Core |
| Database | PostgreSQL (Supabase free tier for MVP) |
| Image Storage | Local disk (`wwwroot/uploads`) for MVP — swappable via `IFileStorageService` |
| Frontend | *(separate repo / folder — handled by the frontend team)* |

---

## Project Structure

```
Reklaim/
├── Reklaim.api/                  # ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── AuthController.cs     # POST /api/auth/register, /login
│   │   ├── ItemPostsController.cs# CRUD for Lost/Found posts + image upload
│   │   └── ClaimsController.cs   # Claim submission, review, phone reveal
│   ├── Data/
│   │   └── AppDbContext.cs       # EF Core DbContext (Identity + app tables)
│   ├── Dtos/
│   │   ├── AuthDtos.cs           # RegisterRequest, LoginRequest
│   │   ├── ItemPostDtos.cs       # CreateItemPostRequest, ItemPostResponse
│   │   └── ClaimDtos.cs          # CreateClaimRequest, ClaimResponse, ApprovedClaimResponse
│   ├── Models/
│   │   ├── User.cs               # Extends IdentityUser<int>
│   │   ├── ItemPost.cs           # Lost/Found item post
│   │   ├── ClaimRequest.cs       # Claim on a post
│   │   └── Enums.cs              # PostType, PostStatus, ClaimStatus
│   ├── Services/
│   │   ├── IFileStorageService.cs           # Abstraction for file uploads
│   │   └── LocalDiskFileStorageService.cs   # MVP: saves to wwwroot/uploads
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs
├── Reklaim.slnx                  # Solution file
├── project-breakdown.md          # Full project spec
└── README.md
```

---

## Team & Roles

| Name | Role |
|---|---|
| Wilfred Adu Otabil | Project Manager |
| Kwakye Ishmael Affum | Backend Lead |
| Ablorh-Adjei Caleb | Backend Developer |
| Shadrack Dorkenoo | Frontend Lead |
| Joel Adom Yaw Opoku | Frontend Developer |
| Samuel Kofi Ntem Amankwah | Frontend Developer |
| Emmanuel Jerry Kuake | Database Admin |
| Kennedy Sarfo | Security & Auth |
| Melchizedek Sensemore D.A | DevOps / Deployment |
| Prince Boateng | QA Lead |
| Prosper Sokari | QA Tester |
| Peter Paul Didemudo | Documentation |

---

## Getting Started (Backend)

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A PostgreSQL database (local or [Supabase](https://supabase.com) free tier)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

### 1. Clone the repository
```bash
git clone <repo-url>
cd Reklaim/Reklaim.api
```

### 2. Set up secrets (never commit these!)
```bash
dotnet user-secrets set "Jwt:Key" "<your-secret-key-min-32-chars>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<host>;Database=<db>;Username=<user>;Password=<pass>"
```

### 3. Apply database migrations
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Run the API
```bash
dotnet run
```

The API will be available at `https://localhost:<port>`. The OpenAPI/Swagger UI is accessible at `https://localhost:<port>/openapi/v1.json` in development.

---

## Environment Variables & Secrets

| Key | Where to Set | Description |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | User Secrets | PostgreSQL connection string |
| `Jwt:Key` | User Secrets | JWT signing key (min 32 chars) |
| `Jwt:Issuer` | `appsettings.Development.json` | `CampusLostAndFoundApi` |
| `Jwt:Audience` | `appsettings.Development.json` | `CampusLostAndFoundClients` |

> ⚠️ **Never commit `Jwt:Key` or your database password to Git.** Always use `dotnet user-secrets` locally.

---

## API Endpoints

### Auth
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | None | Register with `@st.ug.edu.gh` email |
| POST | `/api/auth/login` | None | Login, receive a 1-hour JWT |

### Item Posts (Hub)
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/itemposts` | None | Browse all posts (filter by `type`, `category`, `status`, `search`) |
| GET | `/api/itemposts/{id}` | None | Get a single post |
| POST | `/api/itemposts` | Required | Create a new Lost/Found post (multipart/form-data for image) |
| PATCH | `/api/itemposts/{id}/status` | Required | Update post status (poster only) |
| DELETE | `/api/itemposts/{id}` | Required | Delete post + image (poster only) |

### Claims
| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/claims` | Required | Submit a claim with proof (max 3 pending per user) |
| GET | `/api/claims/{id}` | Required | View a specific claim |
| GET | `/api/claims/my-claims` | Required | All claims you submitted |
| GET | `/api/claims/on-my-posts` | Required | All incoming claims on your posts |
| POST | `/api/claims/{id}/review` | Required | Approve or deny a claim (finder only). Phone numbers revealed on approval. |

---

## Git Workflow

1. Always branch off `main`: `git checkout -b feature/<your-feature-name>`
2. Make your changes and commit with a clear message.
3. Push your branch and open a **Pull Request** for review.
4. At least one other team member must review before merging into `main`.

> **Branch naming examples:** `feature/authentication`, `feature/hub-ui`, `fix/claim-rate-limit`