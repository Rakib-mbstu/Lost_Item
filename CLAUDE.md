# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**StolenTracker** — a full-stack web application for reporting and searching stolen items (mobile phones, bikes, laptops). Users authenticate via Google OAuth; authenticated users can file theft complaints with a police report upload; admins review and approve complaints before they appear in public search results.

## Tech Stack

- **Backend:** ASP.NET Core 9.0 (C#), Entity Framework Core 9.0.3, SQL Server, JWT auth, Google OAuth
- **Frontend:** React 18 + TypeScript, Vite, Axios, `@react-oauth/google`, React Router 6, Tailwind CSS 3

## Commands

### Backend (`Lost_Item/`)

```bash
# dotnet is at /home/mir/.dotnet/dotnet — always set these when running dotnet commands:
export PATH="$PATH:/home/mir/.dotnet"
export DOTNET_ROOT=/home/mir/.dotnet

# Run the backend (auto-migrates DB on startup, listens on http://localhost:5099)
dotnet run

# Apply migrations manually
dotnet ef database update

# Add a new migration
dotnet ef migrations add <MigrationName>

# Build
dotnet build
```

### Frontend (`Lost_Item_frontend/`)

```bash
# Install dependencies
npm install

# Start dev server (http://localhost:5173, proxies /api and /uploads to :5099)
npm run dev

# Build for production
npm run build

# Type check
npx tsc --noEmit
```

## Architecture

### Backend Structure

**TPH (Table-Per-Hierarchy) inheritance** for products: `Product` is the abstract base class; `Mobile`, `Bike`, `Laptop` inherit from it and are stored in a single `Products` table with a `Discriminator` column.

```
Controllers/
  AuthController.cs       — Google OAuth exchange, JWT issuance, user/admin management
  ComplaintsController.cs — File complaint with police report upload; approval workflow; user updates
  SearchController.cs     — Public search by tracking ID (IMEI/FrameNumber/SerialNumber)
Models/
  Product.cs              — Abstract base; TrackingId per subtype (IMEI, FrameNumber, SerialNumber)
  Mobile/Bike/Laptop.cs   — Concrete product types with unique tracking identifiers
  Complaint.cs            — Links User → Product; holds PoliceReportPath, Status, AdminNote
  ComplaintUpdate.cs      — User-posted timestamped messages on a complaint (owner + admin only)
  ComplaintStatus.cs      — Enum: Pending=0, Approved=1, Resolved=2, Rejected=3
  RevokedToken.cs         — JWT blacklist for logout
Services/
  ProductService.cs       — Polymorphic product creation/search
  ComplaintService.cs     — Complaint CRUD, file upload, approval workflow, user updates
  AuthService.cs          — Google token validation, JWT generation/revocation
Data/AppDbContext.cs      — EF context; seeds admin user; configures unique indexes
Uploads/                  — Police report files served as static files at /uploads
```

`Program.cs` wires everything up: SQL Server, JWT Bearer auth, CORS for `http://localhost:5173`, static file serving from `Uploads/`, Swagger at `/swagger`, auto-migration on startup, rate limiting, `AdminOnly` policy.

### API Surface

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/google` | — | Exchange Google ID token for app JWT |
| POST | `/api/auth/logout` | JWT | Revoke current JWT |
| GET | `/api/auth/me` | JWT | Current user info |
| GET | `/api/auth/users` | JWT + Admin | List all users |
| PATCH | `/api/auth/users/{id}/make-admin` | JWT + Admin | Grant/revoke admin |
| GET | `/api/search?trackingId=&type=` | — | Public search by identifier |
| GET | `/api/complaints` | JWT | Admin → all complaints; user → own complaints |
| GET | `/api/complaints/mine` | JWT | Always returns only the current user's complaints |
| POST | `/api/complaints` | JWT | File new complaint (multipart/form-data) |
| PATCH | `/api/complaints/{id}/approve` | JWT + Admin | Approve pending complaint |
| PATCH | `/api/complaints/{id}/reject` | JWT + Admin | Reject pending complaint |
| PATCH | `/api/complaints/{id}/resolve` | JWT + Admin | Resolve approved complaint |
| PATCH | `/api/complaints/{id}/note` | JWT + Admin | Set or clear admin note |
| GET | `/api/complaints/{id}/updates` | JWT (owner or admin) | Get user-posted updates |
| POST | `/api/complaints/{id}/updates` | JWT (owner only) | Post a timestamped update |

### Complaint Lifecycle

```
Pending (default on create)
  → Approved  (admin — visible in public search)
      → Resolved (admin — item recovered, removed from search)
  → Rejected  (admin — dismissed, hidden from search)
```

- Updates can only be posted on `Pending` or `Approved` complaints.
- `AdminNote` can be set/cleared at any status.

### Admin checks

The `AdminOnly` policy and the `IsAdmin()` helper in `ComplaintsController` both use the custom `"isAdmin"` claim (not `ClaimTypes.Role`):

```csharp
// Policy (Program.cs)
policy.RequireClaim("isAdmin", "True")

// Controller helper
private bool IsAdmin() => User.HasClaim("isAdmin", "True");
```

The JWT sets this claim as `user.IsAdmin.ToString()` → `"True"` or `"False"`.

### Frontend Structure

```
src/
  App.tsx                  — Route definitions; wraps app in GoogleOAuthProvider
  context/AuthContext.tsx  — Auth state (JWT, name, email, isAdmin) in localStorage;
                             login() exchanges Google ID token for app JWT
  helper/api.ts            — Axios instance (base /api); interceptor auto-attaches JWT header
  pages/
    SearchPage.tsx          — Public product lookup by tracking ID + type
    LoginPage.tsx           — Google sign-in button
    NewComplaintPage.tsx    — Form at /complaints/new; redirects to /complaints on success
    MyComplaintsPage.tsx    — User's own complaints (/complaints/mine); expandable updates panel
    AdminPage.tsx           — Two-tab dashboard: Complaints (approve/reject/resolve, read-only updates) + Users
    ComplaintsPage.tsx      — Legacy page (kept but superseded by MyComplaintsPage + NewComplaintPage)
  components/
    Navbar.tsx              — Nav with active-page highlight via useLocation(); hamburger drawer on mobile
    Toast.tsx               — useToast hook + ToastContainer; success/error/info variants
```

Vite proxies `/api` and `/uploads` to `http://localhost:5099` (see `vite.config.ts`), so no CORS issues in development.

### Authentication Flow

1. User clicks Google sign-in → Google returns ID token
2. Frontend POSTs token to `POST /api/auth/google`
3. Backend validates with Google APIs, upserts `User`, returns app JWT + user info
4. Frontend stores JWT in `localStorage`, reads it via `AuthContext`
5. All subsequent API requests carry `Authorization: Bearer <token>`
6. Logout calls `POST /api/auth/logout` to blacklist the JWT's `jti` in `RevokedTokens`

### Key Configuration

- `appsettings.json` — `ConnectionStrings:DefaultConnection`, `Jwt:Secret`/`Issuer`/`Audience`, `Google:ClientId`, `AdminEmails` (array — users with these emails are auto-promoted to admin on login)
- `Lost_Item_frontend/.env` — `VITE_GOOGLE_CLIENT_ID`
- Seed admin user is configured in `AppDbContext.cs` `OnModelCreating` with `GoogleId = "ADMIN_SEED_GOOGLE_ID"`

### Database Schema

```
Users            — Id, GoogleId (unique), Email, Name, IsAdmin, CreatedAt
Products (TPH)   — Id, Type (Discriminator), Brand, Model, TrackingId (unique), CreatedAt, UpdatedAt
  Mobile         — + IMEI (unique filtered index)
  Bike           — + FrameNumber (unique), EngineNumber (unique)
  Laptop         — + SerialNumber (unique), MacAddress (unique nullable)
Complaints       — Id, ProductId→Products (cascade), UserId→Users (cascade),
                   LocationStolen (max 200), PoliceReportPath, Status (int),
                   AdminNote (max 1000, nullable), CreatedAt, ReviewedAt?, ResolvedAt?
ComplaintUpdates — Id, ComplaintId→Complaints (cascade), UserId→Users (restrict),
                   Message (max 500), CreatedAt
RevokedTokens    — Id, Jti, UserId→Users, ExpiresAt
```

### Product Tracking IDs

Each product type has a unique identifier used for public search:
- **Mobile** → `IMEI` (unique index)
- **Bike** → `FrameNumber` + `EngineNumber` (separate unique indexes)
- **Laptop** → `SerialNumber` + optional `MacAddress` (unique indexes with null-safe handling via migration `AddUniqueIndexesWithNull`)

### Rate Limiting

Sliding window per IP, configured in `Program.cs`:
- `POST /api/auth/google` — 10 req/min
- `GET /api/search` — 30 req/min
- `POST /api/complaints` — 5 req/min

Returns `429` with `Retry-After: 60` on breach.

### Frontend Design System (Tailwind)

Tokens defined in `tailwind.config.js`:

| Token | Hex | Role |
|-------|-----|------|
| `brand.primary` | `#1E3A8A` | Nav, primary buttons, links |
| `brand.danger`  | `#DC2626` | Stolen badge, errors |
| `brand.accent`  | `#F59E0B` | Pending status, warnings, amber CTAs |
| `brand.bg`      | `#F9FAFB` | Page backgrounds |
| `brand.text`    | `#111827` | Body text |
| `brand.success` | `#16A34A` | Resolved status |
| `brand.card`    | `#FFFFFF` | Card surfaces |
| `brand.muted`   | `#6B7280` | Secondary text |
| `brand.border`  | `#E5E7EB` | Input/card borders |
| `brand.subtle`  | `#EFF6FF` | Blue-tint highlight backgrounds |
