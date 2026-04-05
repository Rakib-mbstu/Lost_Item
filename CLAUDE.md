# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**StolenTracker** — a full-stack web application for reporting and searching stolen items (mobile phones, bikes, laptops). Users authenticate via Google OAuth; authenticated users can file theft complaints; admins can manage users.

## Tech Stack

- **Backend:** ASP.NET Core 9.0 (C#), Entity Framework Core 9.0.3, SQL Server, JWT auth, Google OAuth
- **Frontend:** React 18 + TypeScript, Vite, Axios, `@react-oauth/google`, React Router 6

## Commands

### Backend (`Lost_Item/`)

```bash
# Run the backend (auto-migrates DB on startup, listens on http://localhost:5099)
dotnet run --project Lost_Item/Lost_Item.csproj

# Apply migrations manually
dotnet ef database update --project Lost_Item/Lost_Item.csproj

# Add a new migration
dotnet ef migrations add <MigrationName> --project Lost_Item/Lost_Item.csproj

# Build
dotnet build Lost_Item/Lost_Item.csproj
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
  AuthController.cs      — Google OAuth exchange, JWT issuance, user/admin management
  ComplaintsController.cs — File theft complaints with police report upload
  SearchController.cs    — Public search by tracking ID (IMEI/FrameNumber/SerialNumber)
Models/
  Product.cs             — Abstract base; TrackingId stored per subtype (IMEI, FrameNumber, SerialNumber)
  Mobile/Bike/Laptop.cs  — Concrete product types with unique tracking identifiers
  Complaint.cs           — Links User → Product, holds PoliceReportPath, Status (Open/Resolved)
  RevokedToken.cs        — JWT blacklist for logout
Services/
  ProductService.cs      — Polymorphic product creation/search
  ComplaintService.cs    — Complaint creation, file upload handling
  AuthService.cs         — Google token validation, JWT generation/revocation
Data/AppDbContext.cs     — EF context; seeds admin user; configures unique indexes
Uploads/                 — Police report files served as static files at /uploads
```

`Program.cs` wires everything up: SQL Server, JWT Bearer auth, CORS for `http://localhost:5173`, static file serving from `Uploads/`, Swagger at `/swagger`, auto-migration on startup.

### Frontend Structure

```
src/
  App.tsx                — Route definitions; wraps app in GoogleOAuthProvider
  context/AuthContext.tsx — Auth state (JWT, user info, isAdmin) stored in localStorage;
                            login() exchanges Google ID token for app JWT via /api/auth/google
  helper/api.ts          — Axios instance (base /api); interceptor auto-attaches JWT header
  pages/
    SearchPage.tsx        — Public product lookup by tracking ID + type
    ComplaintsPage.tsx    — Complaint form with product type selection + file upload
    LoginPage.tsx         — Google sign-in button
    AdminPage.tsx         — User list + toggle IsAdmin
  components/Navbar.tsx  — Nav with login/logout
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

- `appsettings.json` — `ConnectionStrings:DefaultConnection`, `Jwt:Secret`/`Issuer`/`Audience`, `GoogleClientId`
- `Lost_Item_frontend/.env` — `VITE_GOOGLE_CLIENT_ID`
- The admin seed user is configured in `AppDbContext.cs` `OnModelCreating`

### Product Tracking IDs

Each product type has a unique identifier used for public search:
- **Mobile** → `IMEI` (unique index)
- **Bike** → `FrameNumber` + `EngineNumber` (separate unique indexes)
- **Laptop** → `SerialNumber` + optional `MacAddress` (unique indexes with null-safe handling via migration `AddUniqueIndexesWithNull`)
