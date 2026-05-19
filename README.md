# ⚖️ LawFlow – Smart Judicial Case Management System

LawFlow is a modern judicial workflow and case management platform built using **ASP.NET Core Blazor Server**, **C#**, **Entity Framework Core**, **SignalR**, and **Supabase PostgreSQL**.

The platform digitizes and streamlines legal and judicial processes through a centralized online portal for:

- Administrators
- Judges
- Lawyers
- Clients
- Police Officers
- Court Clerks

LawFlow focuses on:
- judicial workflow automation
- role-based access control
- real-time communication
- case lifecycle tracking
- document management
- hearing scheduling
- modern SaaS-style dashboards

---

# 🚀 Features

## ✅ Core Features

- Role-based authentication & authorization
- Real-time notifications using SignalR
- Dashboard analytics & charts
- CRUD operations
- Search & filtering system
- Responsive tables
- File upload & document management
- Light/Dark theme switching
- Activity logging system
- Real-time case-based chat
- Responsive SaaS-style UI

---

# 🧠 System Architecture

LawFlow follows a **case-first architecture**.

Everything revolves around:
```text
User → Role → Case → Permissions → Actions
````

The application enforces strict role-based workflows and case-scoped interactions.

---

# 🔄 Case Workflow Lifecycle

```text
Client Creates Case
→ Admin Reviews
→ Judge & Police Assigned
→ Available For Lawyers
→ Lawyer Accepts
→ Clerk Assigned By Judge
→ Investigation
→ Hearings
→ Verdict Issued
→ Case Closed
```

---

# 👥 User Roles

## 🟦 Admin

* Manage users
* Assign judges & police
* Manage cases
* Monitor analytics
* Manage hearings & verdicts

## ⚖️ Judge

* Review assigned cases
* Approve/reject evidence
* Assign clerks
* Issue verdicts

## 👨‍💼 Lawyer

* Accept/decline cases
* Review evidence
* Chat with clients
* View hearings & verdicts

## 🧑 Client

* Submit FIR/cases
* Upload documents
* Track case progress
* Communicate with lawyer

## 👮 Police

* Manage investigations
* Upload reports
* Maintain criminal records

## 🧾 Clerk

* Schedule hearings
* Manage court calendars
* Assist judges
* Publish notifications

---

# 💬 Case-Based Chat System

LawFlow uses a **strict case-scoped communication model**.

### Allowed Chats:

* Client ↔ Lawyer
* Judge ↔ Clerk
* Admin ↔ Police

All chats:

* are linked to a specific CaseId
* use SignalR for real-time messaging
* are only accessible inside case details pages

---

# 📊 Dashboard & Analytics

Each role has a dedicated dashboard containing:

* statistics cards
* visual charts
* activity timelines
* recent cases
* quick action buttons
* notifications panel

### Visualizations Include:

* Bar charts
* Pie charts
* Case status analytics
* Hearing statistics
* Verdict distribution
* Activity summaries

---

# 🎨 UI/UX Design

LawFlow uses a modern SaaS-inspired UI system with:

* glassmorphism cards
* smooth animations
* hover glow effects
* responsive layouts
* rounded components
* sidebar navigation
* theme persistence

### UI Features

* Light/Dark mode
* Animated dashboards
* Responsive design
* Smooth transitions
* Interactive cards & tables

---

# 🛠️ Tech Stack

## Frontend

* Blazor Server
* HTML5
* CSS3
* Bootstrap 5
* MudBlazor
* Chart.js
* Tailwind CSS

## Backend

* ASP.NET Core
* C#
* Entity Framework Core
* SignalR

## Database

* Supabase PostgreSQL
* Npgsql Provider

---

# 🗄️ Database Entities

Main entities include:

* Users
* Roles
* Cases
* CaseAssignments
* Documents
* Hearings
* Verdicts
* Messages
* Notifications
* ActivityLogs
* PoliceReports
* ClerkAssignments

---

# 🔐 Security Features

* ASP.NET Identity authentication
* Role-based authorization
* Secure route protection
* Password hashing
* File upload validation
* Audit logging
* Session management
  
---

# ⚡ Real-Time Features

Using SignalR:

* Real-time notifications
* Live chat updates
* Case status updates
* Hearing schedule updates
* Dashboard refreshes

---

# 📱 Responsive Support

The application is fully responsive and optimized for:

* Desktop
* Tablet
* Mobile browsers

---

# 🎯 Project Goals

LawFlow aims to:

* reduce manual judicial paperwork
* improve legal workflow transparency
* centralize communication
* improve document organization
* modernize judicial operations

---

# 🚀 Future Improvements

Planned future enhancements:

* AI-assisted document analysis
* OCR document scanning
* Email/SMS notifications
* Video hearing support
* E-signature system
* Multi-language support
* Court calendar integrations
* Court Locations

---

# 📌 Status

🟢 In Active Development

---

# 👨‍💻 Developed By

hasbeejay

Built using modern .NET and Blazor technologies.
