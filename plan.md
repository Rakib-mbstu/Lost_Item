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
- [ ] **Search: handle multiple approved complaints** — show count and list of all open complaints in the result card
- [ ] **Complaint detail page** (`/complaints/:id`) — full details and police report preview
- [ ] **Duplicate complaint guard** — warn the user if a product already has a `Pending` or `Approved` complaint
- [ ] **Responsive layout** — current inline styles use fixed widths; needs mobile breakpoints
- [ ] **Loading/error states** — AdminPage has no loading indicator on API calls
- [ ] **Toast notifications** — replace silent failures with a toast system

### Phase 4 — Security & Production

- [ ] **CORS** — lock down to production domain
- [ ] **File storage** — move uploads from local `Uploads/` to cloud storage (S3/Azure Blob)
- [ ] **File size limit** — enforce max upload size on the backend
- [ ] **Rate limiting** — `POST /api/complaints` and `POST /api/auth/google`
- [ ] **Admin policy** — replace manual `if (!IsAdmin())` with `[Authorize(Policy = "AdminOnly")]`

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
