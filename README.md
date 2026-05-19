# LawFlow

LawFlow is a role-secure judicial operations platform built with **Blazor Server** and **PostgreSQL**.  
It manages the full legal case lifecycle: FIR filing, official assignments, investigation, hearings, verdict issuance, and case closure.

## Highlights
- End-to-end case lifecycle with a strict status machine (`Created` -> `Closed`)
- Role-aware experience for `Admin`, `Judge`, `Lawyer`, `Client`, `Police`, and `Clerk`
- Real-time two-party case chat via SignalR channels:
  - `Client <-> Lawyer`
  - `Judge <-> Clerk`
  - `Admin <-> Police`
- Hearing scheduling and workflow notifications
- Evidence/document upload and judge approval flow
- Verdict issuance + clerk publication pipeline
- Dashboard analytics (KPIs + trend and verdict distribution charts)
- Audit logs and in-app notification center
- Custom themed UI system (Tailwind + custom components) with MudBlazor compatibility

## Tech Stack
- **Backend/UI:** ASP.NET Core Blazor Server (`net10.0`)
- **Database:** PostgreSQL via Entity Framework Core + Npgsql
- **Auth:** Cookie auth + custom `AuthenticationStateProvider`
- **Realtime:** SignalR (`/casechathub`, `/notificationhub`)
- **UI:** Tailwind CSS + custom component system + MudBlazor

## Core Workflow
The project uses this status pipeline:

1. `Created` (FIR lodged)
2. `ReviewedByAdmin`
3. `AssignedToJudgeAndPolice`
4. `AvailableForLawyers`
5. `LawyerAccepted`
6. `AssignedToLawyer`
7. `ClerkAssignedByJudge`
8. `Investigation`
9. `Hearing`
10. `VerdictIssued`
11. `Closed`

## Project Structure
```text
LawFlow/
├─ Components/
│  ├─ Layout/                 # Main shell, sidebar, top bar
│  ├─ Pages/                  # Route pages by domain and role
│  └─ Shared/UI/              # Custom reusable UI components
├─ Services/                  # Business logic (cases, docs, hearings, verdicts, chat...)
├─ Models/                    # Domain models + enums
├─ Data/                      # EF DbContext
├─ Hubs/                      # SignalR hubs
├─ Authentication/            # Session model + custom auth state provider
├─ Migrations/                # EF Core migrations
├─ Styles/                    # Tailwind input stylesheet
└─ Program.cs                 # DI, middleware, migrations, startup seeding hooks
```

## Routing Overview
Primary routes include:
- ` /login`
- ` /dashboard`, `/{role}/dashboard`
- ` /cases`, role-specific case routes, and case workspace routes (`/{role}/cases/{id}`)
- ` /hearings`, role hearing pages
- ` /chat`, ` /chat/{caseId}`
- Role areas such as:
  - `/admin/*`
  - `/judge/*`
  - `/lawyer/*`
  - `/client/*`
  - `/police/*`
  - `/clerk/*`

## Local Setup
### 1) Prerequisites
- .NET SDK compatible with `net10.0`
- Node.js (for Tailwind build)
- PostgreSQL (or Supabase Postgres)

### 2) Clone and install
```bash
git clone https://github.com/hasbeejay/LawFlow.git
cd LawFlow
npm install
```

### 3) Configure database
Set either:
- `ConnectionStrings:DefaultConnection` in `appsettings.json`, or
- `DATABASE_URL` environment variable

Example standard connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=lawflow;Username=postgres;Password=your_password"
}
```

### 4) Apply migrations
```bash
dotnet ef database update
```

### 5) Run
```bash
dotnet run
```
Open `http://localhost:5137` (or the URL shown in terminal).

## Demo Data / Seeding
On startup, migrations run automatically. Optional seed hooks are available in `Program.cs`:
- `AuthService.SeedDemoDataAsync()`
- `CaseService.SeedDemoCasesAsync()`

Enable by passing config key `SeedDemoData=true` at run time:
```bash
SeedDemoData=true dotnet run
```

> Important: the demo seed routines are destructive (they clear existing app data before reseeding).

## Test Accounts
Default seed password:
- `Password123!`

Generated account sheet in repo:
- `test_accounts.csv` (contains username/email/role/password)

## Frontend Build
Tailwind is compiled from `Styles/lf.css` to `wwwroot/css/lf.css`.

Useful scripts:
```bash
npm run build:css
npm run watch:css
```

## Security/Behavior Notes
- Role-gated navigation and route authorization are enforced in UI and service layer.
- Chat access is validated server-side per case participant + channel mapping.
- Soft delete filters are applied globally for core entities.
- Notifications are stored server-side and surfaced in the top-bar notification center.

## Known Maintenance Notes
- Build currently surfaces an advisory warning for `Npgsql 8.0.2` (NU1903 / GHSA-x9vc-6hfv-hg8c). Consider upgrading dependency versions.

## Contributing
1. Create a feature branch.
2. Keep service-layer changes paired with route/UI tests.
3. Run:
```bash
dotnet build
```
4. Open a PR with a clear scope and screenshots for UI changes.

## License
No license file is currently included. Add a `LICENSE` file before public/open-source distribution.
