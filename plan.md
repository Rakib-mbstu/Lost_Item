# StolenTracker — Plan

## What the app does

A public registry for stolen items. Anyone can search a device identifier (IMEI, frame/engine number, serial number) to see if it has been reported stolen. Authenticated users (Google login) can file theft complaints with a police report upload. Admins review and approve complaints before they appear in public search results.

---

## Current State

### Working
- Google OAuth login → JWT issuance and revocation
- Public search by identifier — only surfaces `Approved` complaints; `Resolved` products return 404; not-found returns 404
- Complaint filing (authenticated): creates Product + uploads police report, starts as `Pending`
- Complaint approval workflow: `Pending → Approved → Resolved` or `Pending → Rejected` (admin only)
- JWT blacklist on logout
- Admin: grant/revoke admin role via `PATCH /api/auth/users/{id}/make-admin`
- Admin dashboard: two-tab UI — Complaints (filter by status, approve/reject/resolve) + Users
- Admin complaint cards show full details: product brand/model/type chip, labelled identifier (IMEI/Frame No./Serial No.), reporter name + email, location, timestamps, police report link
- My Complaints page: user's own complaints with status label, identifier, location, status note, timeline
- New Complaint page: dedicated form at `/complaints/new`, redirects to `/complaints` on success
- Search result card shows product type chip; "REPORTED STOLEN" badge only if approved; identifier not exposed to public

---

## Fixed Bugs & Issues

| # | Issue | Fix |
|---|-------|-----|
| 1 | `productType` missing from FormData on complaint submit | Appended before submit |
| 2 | Resolve button visible to everyone | Restricted to `user?.isAdmin` |
| 3 | `[Authorize]` commented out on `GET /api/complaints` | Re-added; non-admins see only their own |
| 4 | AdminPage called non-existent `/api/admin/products` | Rewritten as two-tab dashboard |
| 5 | `ComplaintStatus` missing `Pending`/`Rejected` values | New enum: `Pending=0, Approved=1, Resolved=2, Rejected=3` |
| 6 | `TrackingId` set to random GUID for old data | DB migrated; service uses real identifier field |
| 7 | `SearchResult.Type` serialised as integer | Changed to `string`, mapped with `.ToString()` |
| 8 | Duplicate IMEI `121321321` in DB | Duplicate product and complaint removed |
| 9 | Resolved products still showing in search | `SearchByIdentifierAsync` returns `null` when only resolved complaints exist |
| 10 | Complaint view showed random hex as identifier | `GetDisplayId` reads from `IMEI`/`FrameNumber`/`SerialNumber` directly |

---

## Complaint Lifecycle

```
Pending (default on create)
  → Approved  (admin — visible in public search)
      → Resolved (admin — item recovered, removed from search)
  → Rejected  (admin — dismissed, hidden from search)
```

Visibility rules:
- Public search: `Approved` only; `Resolved` → 404
- My Complaints: all statuses for the authenticated owner
- Admin dashboard: all statuses, filterable by tab

---

## API Surface

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/google` | — | Exchange Google ID token for app JWT |
| POST | `/api/auth/logout` | JWT | Revoke current JWT |
| GET | `/api/auth/me` | JWT | Current user info |
| GET | `/api/auth/users` | JWT + Admin | List all users |
| PATCH | `/api/auth/users/{id}/make-admin` | JWT + Admin | Grant/revoke admin |
| GET | `/api/search?trackingId=&type=` | — | Search by identifier |
| GET | `/api/complaints` | JWT | Admin sees all; user sees own |
| POST | `/api/complaints` | JWT | File new complaint (multipart) |
| PATCH | `/api/complaints/{id}/approve` | JWT + Admin | Approve pending → visible in search |
| PATCH | `/api/complaints/{id}/reject` | JWT + Admin | Reject pending complaint |
| PATCH | `/api/complaints/{id}/resolve` | JWT + Admin | Resolve approved complaint |

---

## Planned Work

### Phase 3 — UX & Polish

- [x] **My Complaints page** — `/complaints` shows user's own complaints with status labels, product details, and timeline
- [x] **Search: multiple approved complaints** — result card lists all open complaints with location + date
- [x] **Duplicate complaint guard** — backend returns 409 if product already has a Pending/Approved complaint; frontend shows orange warning; existing product reused on re-file
- [x] **Loading/error states** — AdminPage has spinner and error banner on both tabs
- [x] **Toast notifications** — `Toast.tsx` + `useToast` hook; replaces all `alert()` in AdminPage with success/error toasts
- [x] **Input sanitizer** — global `TrimStringInputFilter` trims all string form/query params; reflects string properties on body DTOs
- [x] **Admin dashboard stats** — stats row at top of AdminPage shows live counts (Total, Pending, Approved, Resolved, Rejected, Users); data fetched once at parent level and passed to tabs
- [x] **Input length limits** — `maxLength` on all frontend inputs; `[StringLength]` on all controller params; `[MaxLength]` on all model properties; `AddFieldLengthLimits` migration narrows columns in DB
- [x] **Admin policy** — `[Authorize(Policy = "AdminOnly")]` on all admin endpoints; `AdminOnly` policy registered in `Program.cs`
- [ ] **Complaint detail page** (`/complaints/:id`) — full details and police report preview
- [ ] **Responsive layout** — see Phase 5 below

### Phase 4 — Security & Production

- [ ] **CORS** — lock down to production domain
- [ ] **File storage** — move uploads from local `Uploads/` to cloud storage (S3/Azure Blob)
- [x] **File size limit** — 10 MB enforced in `ComplaintService`; `[RequestSizeLimit]` can be added if needed
- [x] **Rate limiting** — sliding window per IP: `POST /api/auth/google` (10/min), `GET /api/search` (30/min), `POST /api/complaints` (5/min); 429 with `Retry-After: 60`

---

### Phase 5 — Mobile-Friendly Frontend (Tailwind CSS) ✅

Fully complete. All inline styles removed; every page is responsive and uses the design system below.

#### Design system — `tailwind.config.js`

| Token | Hex | Role |
|-------|-----|------|
| `brand.primary` | `#1E3A8A` | Nav, primary buttons, links |
| `brand.danger`  | `#DC2626` | Stolen badge, errors, destructive actions |
| `brand.accent`  | `#F59E0B` | Pending status, warnings, amber CTAs |
| `brand.bg`      | `#F9FAFB` | Page backgrounds |
| `brand.text`    | `#111827` | Body text |
| `brand.success` | `#16A34A` | Resolved status |
| `brand.card`    | `#FFFFFF` | Card surfaces |
| `brand.muted`   | `#6B7280` | Secondary text, placeholders |
| `brand.border`  | `#E5E7EB` | Input / card borders |
| `brand.subtle`  | `#EFF6FF` | Blue-tint highlight backgrounds |

#### Completed

- [x] **Install Tailwind** — `tailwindcss@3`, `postcss`, `autoprefixer`; config with brand tokens; `index.css` with base body styles
- [x] **`Navbar.tsx`** — deep blue bar, amber CTA, hamburger drawer on mobile
- [x] **`LoginPage.tsx`** — centered card with icon, Google sign-in
- [x] **`SearchPage.tsx`** — blue hero banner, floating search card, red stolen banner on result
- [x] **`NewComplaintPage.tsx`** — blue page header, white form card, styled file input, amber/red inline feedback
- [x] **`MyComplaintsPage.tsx`** — status dot + colored badge per complaint, wrapping timeline row
- [x] **`AdminPage.tsx`** — tinted stat cards (`grid-cols-2→3→6`), white complaint cards with gray header strip, users table with role pill, `Joined` hidden on mobile
- [x] **`Toast.tsx`** — light tinted bg + left border accent per type (success/error/info)

---

## Database Schema

```
Users           — Id, GoogleId (unique), Email, Name, IsAdmin, CreatedAt
Products (TPH)  — Id, Type, Brand, Model, TrackingId (unique), CreatedAt, UpdatedAt
  Mobile        — + IMEI (unique filtered index)
  Bike          — + FrameNumber (unique), EngineNumber (unique)
  Laptop        — + SerialNumber (unique), MacAddress (unique, nullable)
Complaints      — Id, ProductId→Products, UserId→Users, LocationStolen,
                  PoliceReportPath, Status, CreatedAt, ReviewedAt?, ResolvedAt?
RevokedTokens   — Id, Jti, UserId→Users, ExpiresAt
```
