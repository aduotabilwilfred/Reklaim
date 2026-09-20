# Campus Lost & Found — Project Breakdown

**Platform:** Web Application
**Stack:** ASP.NET Core (MVC/Blazor) + ASP.NET Core Web API + SQL Server/PostgreSQL (EF Core) + Azure Blob Storage / AWS S3

---

## 1. Core Components

### 1.1 User Authentication
- Students sign in using their **university email** (e.g., `@student.edu`) to prevent spam and ensure accountability.
- Email domain validation on registration.
- Optional: email verification link/OTP before account activation.

### 1.2 The Hub (Feed)
- Searchable, filterable dashboard.
- Two distinct streams:
  - **Lost Items**
  - **Found Items**
- Filters: category, date posted, location, status.
- Search: keyword search across title/description.

### 1.3 Image Upload System
- Finders can snap a quick photo when creating a post.
- For the MVP: images are stored on **local disk** (no cost), accessed behind a small `IFileStorageService` abstraction.
- This keeps the door open to swap in Azure Blob Storage or AWS S3 later without changing the rest of the app.
- Used for visual verification by claimants.

### 1.4 Claiming & Verification System
- No public phone numbers.
- Initial approach: **verification ticket + reveal contact**.
  - Claimant clicks "Claim" and submits a `ClaimRequest` with a `ProofDescription` (e.g., a detail only the true owner would know).
  - Finder reviews the ticket and Approves or Denies it.
  - On Approval, the claimant's phone number (and the finder's) is revealed — no email or other contact info — so they can coordinate the handoff directly.
  - In-app messaging can be added later as a v2 upgrade, replacing the "reveal contact" step.

---

## 2. Tech Stack (.NET Ecosystem — Web)

| Component | Choice | MVP Notes |
|---|---|---|
| Frontend & UI | ASP.NET Core MVC or Blazor Web App | Free |
| Backend / API | ASP.NET Core Web API | Free |
| Database | PostgreSQL via Entity Framework Core | Run locally, or free tier (Supabase/Neon/Railway) |
| Image Storage | Local disk for MVP, behind `IFileStorageService` | No cost; swap to Azure Blob/S3 later if needed |

---

## 3. Key Database Tables (EF Core Models)

### 3.1 `User`
| Field | Type | Notes |
|---|---|---|
| Id | int/GUID | Primary key |
| Name | string | |
| StudentEmail | string | Must match the university's email domain, unique |
| PhoneNumber | string | Private — never exposed publicly |
| DateJoined | DateTime | |

### 3.2 `ItemPost`
| Field | Type | Notes |
|---|---|---|
| Id | int/GUID | Primary key |
| Title | string | |
| Description | string | |
| LocationFound | string | |
| Category | enum/string | Electronics, Keys, Documents, etc. |
| PostType | enum | Lost or Found |
| ImageUrl | string | Points to cloud storage object |
| DatePosted | DateTime | |
| Status | enum | Active, Claimed, Resolved |
| UserId | FK → User.Id | Poster of the item |

### 3.3 `ClaimRequest`
| Field | Type | Notes |
|---|---|---|
| Id | int/GUID | Primary key |
| PostId | FK → ItemPost.Id | |
| ClaimerUserId | FK → User.Id | |
| ProofDescription | string | Claimant's evidence of ownership |
| Status | enum | Pending, Approved, Denied |

**Rate limiting:** cap each user to a maximum of **3 open/pending claims at a time** (across all posts), to prevent spam/abuse of the reveal-contact flow. Once a claim is Approved or Denied, it no longer counts toward the limit.

---

## 4. Suggested Build Phases

1. **Foundation**
   - Set up ASP.NET Core Web API + EF Core + database.
   - Implement `User`, `ItemPost`, `ClaimRequest` models and migrations.
2. **Authentication**
   - University email sign-up/login (e.g., ASP.NET Core Identity + domain restriction, or email OTP).
3. **The Hub (Feed)**
   - Build Lost/Found feed views with search and filters.
4. **Image Uploads**
   - Build `IFileStorageService` abstraction; implement a local-disk version for the MVP (no cost).
   - Store the returned file path/URL in `ItemPost.ImageUrl`.
   - Swap in Azure Blob Storage/AWS S3 later behind the same interface if the app needs to scale.
5. **Claiming & Verification**
   - Build claim workflow: submit `ClaimRequest` (with proof description) → notify finder → finder approves/denies → on approval, reveal phone numbers to both parties.
   - Enforce rate limit: max 3 open/pending claims per user at a time.
6. **Polish & Deploy**
   - Status management (Active/Claimed/Resolved), notifications, deployment to Azure/AWS.

---

## 5. Open Questions to Resolve Before Building

- Hosting preference (deferred): Azure or AWS — for app hosting and image storage. See note below on why this can wait and what it does affect.

### A note on deferring the hosting decision
Hosting can safely be decided later — it won't block backend/frontend/database development. It mainly affects:
- **Image Storage integration code**: the Azure Blob Storage SDK and AWS S3 SDK have different APIs, so whichever you pick determines the exact upload/download code in the Image Upload System. Wrapping this behind a small interface (e.g., `IFileStorageService` with `UploadAsync`/`GetUrlAsync`) lets you build everything else now and plug in the real provider later with minimal rework.
- **Deployment/CI setup**: choosing later just delays setting up the deployment pipeline, not the app logic itself.
- **Cost/ops details**: pricing, region selection, and access-control setup differ between providers, but neither blocks early development.

### MVP: keep it free
Since this is an MVP, avoid any paid services for now:
- **Image storage**: store uploaded files on local disk (behind `IFileStorageService`) instead of Azure Blob/S3 — no cost, swap in cloud storage later if needed.
- **Database**: run PostgreSQL locally, or use a free tier from a host like Supabase, Neon, or Railway.
- **App hosting**: not needed yet; when you do deploy, free tiers exist (Render, Railway, Fly.io, Azure App Service free tier).
- **Auth**: ASP.NET Core Identity is free and self-hosted, so no third-party auth costs.

Practical suggestion: build locally against the filesystem for image storage now, and swap in a cloud provider behind that interface only if/when the MVP needs to scale beyond free-tier limits.
