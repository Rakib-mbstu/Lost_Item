# StolenTracker

A full-stack web application for reporting and searching stolen items — mobile phones, bikes, and laptops. Anyone can search by a device identifier (IMEI, frame number, serial number) to check if it has been reported stolen. Authenticated users can file theft complaints with a police report upload. Admins review and approve complaints before they appear in public search results.

---

## Features

- **Public search** — look up any device by IMEI, frame/engine number, or serial number
- **Google OAuth login** — no passwords; sign in with your Google account
- **Complaint filing** — authenticated users submit theft reports with a police report file (PDF/JPG/PNG)
- **Approval workflow** — complaints start as *Pending*; admins approve, reject, or resolve them
- **Admin dashboard** — two-tab UI: manage complaints (filter by status, take actions) and manage users (grant/revoke admin)
- **JWT authentication** — stateless auth with a token blacklist on logout

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 9.0, Entity Framework Core 9.0, SQL Server |
| Auth | Google OAuth 2.0, JWT Bearer |
| Frontend | React 18, TypeScript, Vite, Axios |
| Styling | Inline styles (no CSS framework) |

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- SQL Server (local or Docker)
- A [Google Cloud Console](https://console.cloud.google.com/) project with OAuth 2.0 credentials

### 1. Clone the repository

```bash
git clone https://github.com/your-username/StolenTracker.git
cd StolenTracker
```

### 2. Configure the backend

```bash
cp Lost_Item/appsettings.example.json Lost_Item/appsettings.json
```

Edit `Lost_Item/appsettings.json` and fill in:

| Key | Description |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Jwt:Secret` | Random string, at least 32 characters |
| `Google:ClientId` | OAuth 2.0 Client ID from Google Cloud Console |
| `AdminEmails` | List of emails that get admin role on first login |

### 3. Configure the frontend

```bash
cp Lost_Item_frontend/.env.example Lost_Item_frontend/.env
```

Edit `Lost_Item_frontend/.env`:

| Variable | Description |
|---|---|
| `VITE_GOOGLE_CLIENT_ID` | Same Google OAuth Client ID as above |

### 4. Run the backend

```bash
cd Lost_Item
dotnet run --project Lost_Item.csproj
```

The backend starts on `http://localhost:5099`. It **auto-migrates the database on startup**, so no manual `dotnet ef database update` is needed.

Swagger UI is available at `http://localhost:5099/swagger`.

### 5. Run the frontend

```bash
cd Lost_Item_frontend
npm install
npm run dev
```

The frontend starts on `http://localhost:5173` and proxies `/api` and `/uploads` to the backend automatically.

---

## Google OAuth Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/) → **APIs & Services** → **Credentials**
2. Create an **OAuth 2.0 Client ID** (Web application type)
3. Add `http://localhost:5173` to **Authorised JavaScript origins**
4. Copy the **Client ID** into both config files above

---

## Project Structure

```
StolenTracker/
├── Lost_Item/                  # ASP.NET Core backend
│   ├── Controllers/            # AuthController, ComplaintsController, SearchController
│   ├── Data/                   # AppDbContext, EF migrations
│   ├── DTOs/                   # Request/response records
│   ├── Models/                 # Product (TPH), Complaint, User, RevokedToken
│   ├── Services/               # AuthService, ProductService, ComplaintService
│   ├── Uploads/                # Police report files (gitignored)
│   └── appsettings.example.json
└── Lost_Item_frontend/         # React + TypeScript frontend
    ├── src/
    │   ├── components/         # Navbar
    │   ├── context/            # AuthContext (JWT + user state)
    │   ├── helper/             # Axios instance
    │   └── pages/              # SearchPage, MyComplaintsPage, NewComplaintPage,
    │                           #   AdminPage, LoginPage
    └── .env.example
```

## Data Model

```
Users           — Id, GoogleId, Email, Name, IsAdmin, CreatedAt
Products (TPH)  — Id, Type, Brand, Model, TrackingId, CreatedAt, UpdatedAt
  Mobile        — + IMEI (unique)
  Bike          — + FrameNumber (unique), EngineNumber (unique)
  Laptop        — + SerialNumber (unique), MacAddress (unique, nullable)
Complaints      — Id, ProductId, UserId, LocationStolen, PoliceReportPath,
                  Status, CreatedAt, ReviewedAt?, ResolvedAt?
RevokedTokens   — Id, Jti, UserId, ExpiresAt
```

### Complaint lifecycle

```
Pending  →  Approved  →  Resolved
         ↘  Rejected
```

- **Pending** — newly filed, not yet reviewed; hidden from public search
- **Approved** — admin approved; visible in public search as "REPORTED STOLEN"
- **Resolved** — item recovered; removed from public search
- **Rejected** — admin dismissed; hidden from public search

---

## API Reference

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/google` | — | Exchange Google ID token for app JWT |
| POST | `/api/auth/logout` | JWT | Revoke current token |
| GET | `/api/auth/me` | JWT | Current user info |
| GET | `/api/auth/users` | Admin | List all users |
| PATCH | `/api/auth/users/{id}/make-admin` | Admin | Grant/revoke admin role |
| GET | `/api/search?trackingId=&type=` | — | Search by identifier |
| GET | `/api/complaints` | JWT | Own complaints (admin sees all) |
| POST | `/api/complaints` | JWT | File a new complaint (multipart/form-data) |
| PATCH | `/api/complaints/{id}/approve` | Admin | Approve a pending complaint |
| PATCH | `/api/complaints/{id}/reject` | Admin | Reject a pending complaint |
| PATCH | `/api/complaints/{id}/resolve` | Admin | Resolve an approved complaint |

---

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Commit your changes: `git commit -m "Add your feature"`
4. Push to the branch: `git push origin feature/your-feature`
5. Open a pull request

---

## License

MIT
