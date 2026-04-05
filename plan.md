# StolenTracker — App Plan

## What the app does

A public registry for stolen items. Anyone can search a device identifier (IMEI, frame/engine number, serial number) to see if it has been reported stolen. Authenticated users (Google login) can file theft complaints with a police report upload. Admins can manage users and review/approve/reject/resolve complaints.

---

## Current State

### Working
- Google OAuth login → JWT issuance and revocation
- Public search by tracking ID + product type — only surfaces `Approved` complaints; returns 404 when not stolen
- Complaint creation (authenticated): creates Product + uploads police report file, starts as `Pending`
- Complaint approval workflow: `Pending → Approved → Resolved` or `Pending → Rejected` (admin only)
- JWT blacklist on logout
- Admin: grant/revoke admin role via `PATCH /api/auth/users/{id}/make-admin`
- Admin dashboard: two-tab UI — Complaints (filter by status, approve/reject/resolve) + Users
- Admin complaint view shows full details: product brand/model/type, real identifier, reporter name + email, location, timestamps, police report link
- Search result shows the real type-specific identifier (IMEI / FrameNumber / SerialNumber) — not a stored TrackingId that may be stale

---

### Fixed Bugs

| # | Location | Issue | Fix |
|---|----------|-------|-----|
| 1 | `ComplaintsPage.tsx:68` | `productType` missing from FormData | Appended to form before submit |
| 2 | `ComplaintsPage.tsx:153` | Resolve button visible to everyone | Restricted to `user?.isAdmin` only |
| 3 | `ComplaintsController.cs:23` | `[Authorize]` commented out on `GET /api/complaints` | Re-added; non-admins see only their own complaints |
| 4 | `AdminPage.tsx` | Called non-existent `/api/admin/products` endpoints | Rewritten to use existing auth + complaints endpoints |
| 5 | `AdminPage.tsx` | Product management instead of user management | Rewritten as two-tab dashboard (Complaints + Users) |

---

### Fixed via Feedback (latest round)

| # | Feedback | Fix |
|---|----------|-----|
| F1 | Admin should see more complaint details | `ComplaintResponse` extended with `ProductBrand`, `ProductModel`, `ProductType`, `UserEmail`; `AdminPage` card updated |
| F2 | Tracking ID inconsistent between old and new data | `SearchByIdentifierAsync` now reads the actual type-specific field (`IMEI` / `FrameNumber` / `SerialNumber`) instead of the stored `TrackingId` |
| F3 | No need to show "not reported stolen" | Search returns `404` when no approved complaint exists; frontend only renders result card for stolen items |

---

## Implementation Summary

### Backend changes applied

1. **`ComplaintStatus` enum** — `Pending = 0`, `Approved = 1`, `Resolved = 2`, `Rejected = 3`
2. **`Complaint.cs`** — added `ReviewedAt DateTime?`; default status is `Pending`
3. **`AddComplaintReviewedAt` migration** — adds `ReviewedAt` column
4. **`AddUniqueIndexesWithNull` migration** — replaces plain unique index on `MacAddress` with a filtered index (`WHERE MacAddress IS NOT NULL`)
5. **`ProductService.CreateAsync`** — `TrackingId` set from real identifier, not a random GUID
6. **`ProductService.SearchByIdentifierAsync`** — returns `null` (→ 404) when no approved complaints; `displayId` derived from type-specific field
7. **`ComplaintService`** — `ResolveAsync` admin-only; `ApproveAsync` and `RejectAsync` added; `MapToResponse` includes product brand/model/type and user email
8. **`ComplaintsController`** — `[Authorize]` on `GetAll`; added `Approve` / `Reject` endpoints; `Resolve` admin-only
9. **`ComplaintResponse` DTO** — added `ProductBrand`, `ProductModel`, `ProductType`, `UserEmail`
10. **`SearchController`** — returns `NotFound` when result is null

### Frontend changes applied

1. **`ComplaintsPage.tsx`** — `productType` appended to FormData; resolve button restricted to admins; status labels updated for new enum values
2. **`AdminPage.tsx`** — rewritten as two-tab dashboard; complaint card shows product details, reporter email, all timestamps
3. **`SearchPage.tsx`** — result card only shown when item is stolen; 404 → "No stolen report found"; label changed from "Tracking ID" to "Identifier"; unused imports removed

---

## Planned Work

### Phase 3 — UX & Polish

- [ ] **Search: handle multiple approved complaints** — show count and list of all approved open complaints in the result card
- [ ] **Complaint detail page** (`/complaints/:id`) — full details and police report preview, accessible to the reporter and admins
- [x] **My Complaints page** — `/complaints` shows user's own complaints with status labels, product details, and timeline; `/complaints/new` holds the filing form
- [ ] **Duplicate complaint guard** — warn the user if a product tracking ID already has a `Pending` or `Approved` complaint
- [ ] **Responsive layout** — current inline styles use fixed widths (`maxWidth: 800`); needs mobile breakpoints
- [ ] **Loading/error states** — AdminPage has no loading indicator or error handling on any API call
- [ ] **Toast notifications** — replace silent failures with a toast system for submit success/error feedback

### Phase 4 — Security & Production

- [ ] **CORS** — lock down to production domain; remove `http://localhost:5173` from production config
- [ ] **File storage** — move uploaded police reports from local `Uploads/` directory to cloud storage (S3/Azure Blob)
- [ ] **File size limit** — enforce a max upload size (e.g. 5 MB) on the backend
- [ ] **Rate limiting** — add rate limiting to `POST /api/complaints` and `POST /api/auth/google`
- [ ] **Admin endpoint authorization** — replace manual `if (!IsAdmin()) return Forbid()` checks with `[Authorize(Policy = "AdminOnly")]`

---

## API Surface

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/google` | — | Exchange Google ID token for app JWT |
| POST | `/api/auth/logout` | JWT | Revoke current JWT |
| GET | `/api/auth/me` | JWT | Current user info |
| GET | `/api/auth/users` | JWT + Admin | List all users |
| PATCH | `/api/auth/users/{id}/make-admin` | JWT + Admin | Grant/revoke admin |
| GET | `/api/search?trackingId=&type=` | — | Search by identifier — 404 if not stolen |
| GET | `/api/complaints` | JWT | Admin sees all; user sees own |
| POST | `/api/complaints` | JWT | File new complaint (multipart) |
| PATCH | `/api/complaints/{id}/approve` | JWT + Admin | Approve pending → visible in search |
| PATCH | `/api/complaints/{id}/reject` | JWT + Admin | Reject pending complaint |
| PATCH | `/api/complaints/{id}/resolve` | JWT + Admin | Resolve approved complaint |

---

## Database Schema

```
Users           — Id, GoogleId (unique), Email, Name, IsAdmin, CreatedAt
Products (TPH)  — Id, ProductType (discriminator), Brand, Model, TrackingId (unique), CreatedAt, UpdatedAt
  Mobile        — + IMEI (unique index)
  Bike          — + FrameNumber (unique), EngineNumber (unique)
  Laptop        — + SerialNumber (unique), MacAddress (filtered unique index — NULL allowed)
Complaints      — Id, ProductId→Products, UserId→Users, LocationStolen, PoliceReportPath,
                  Status (Pending/Approved/Resolved/Rejected), CreatedAt, ReviewedAt?, ResolvedAt?
RevokedTokens   — Id, Jti, UserId→Users, ExpiresAt
```

**Status lifecycle:**
```
Pending (default on create)
  → Approved  (admin action — complaint visible in search)
      → Resolved (admin action — item recovered, removed from search)
  → Rejected  (admin action — complaint dismissed, not visible in search)
```

Visibility rules:
- Public search: `Approved` only
- Reporter (own complaints): all statuses
- Admin dashboard: all statuses, filterable by tab




