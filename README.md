# MIRAS-PRO-Digital-Inheritance-and-Estate-Dispute-Tracker-
# MirasPro

A Blazor Server web application for managing Islamic inheritance (miras) cases, built for a Pakistani legal context.

![.NET](https://img.shields.io/badge/.NET-Blazor%20Server-blueviolet)
![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red)
![Status](https://img.shields.io/badge/Status-In%20Development-yellow)

## Overview

MirasPro helps track and process inheritance-related records through a structured admin workflow — from case submission and queuing to review and provisioning. It's built with a focus on secure session handling, role-based admin operations, and a clean, accessible UI.

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | Blazor Server (.NET) |
| Database | SQL Server |
| ORM | Entity Framework Core |
| Auth / Security | BCrypt (password hashing), ProtectedSessionStorage |
| Styling | Custom CSS (WCAG-compliant) |

## Features

- **Admin Console** — central dashboard for managing inheritance cases
- **FRC Workflow** — Queue, Detail, and Submit pages for handling case records end-to-end
- **Admin Provisioning & Registration** — dedicated flows for setting up and registering admin accounts
- **Secure Session Handling** — session state guarded via `ProtectedSessionStorage`, with auth checks run in `OnAfterRenderAsync` to avoid race conditions common in Blazor Server apps
- **Accessible, Polished UI** — CSS rewritten across the app to meet WCAG contrast standards, with consistent CSS variables and semantic color usage
- **Hero Section** — custom sunset/house background image with a neutral scrim overlay for readability

## Project Structure

```
MirasPro/
├── Components/
│   ├── Pages/          # Razor pages (AdminConsole, FrcQueue, FrcDetail, etc.)
│   └── Layout/          # Shared layout & sidebar
├── Services/             # UserService, DashboardService, etc.
├── Data/                 # AppDbContext, EF Core models
├── wwwroot/              # Static assets (CSS, images)
├── appsettings.json       # Configuration (connection strings)
└── Program.cs             # App entry point & service registration
```

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (matching the project's target version)
- SQL Server (LocalDB, Express, or full instance)
- Visual Studio 2022 / VS Code

## Getting Started

1. **Clone the repo**
   ```bash
   git clone <repo-url>
   cd MirasPro
   ```

2. **Configure the database connection**
   Update `appsettings.json` with your SQL Server connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=YOUR_SERVER;Database=MirasProDB;Trusted_Connection=True;TrustServerCertificate=True"
   }
   ```

3. **Apply migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the app**
   ```bash
   dotnet run
   ```

## Known Fixes & Learnings

- Fixed a critical `ProtectedSessionStorage` race condition by moving auth guards from `OnInitializedAsync` to `OnAfterRenderAsync`
- Fixed a non-functional logout button caused by a missing `@rendermode` directive on the sidebar component
- Conducted a full CSS audit across ~16 files, resolving contrast failures and inconsistent styling

## Project History

MirasPro evolved from earlier prototypes of the same inheritance/estate tracking concept:

1. **EstateRegistry** — ASP.NET Core MVC, server-side Razor, raw ADO.NET, no JavaScript
2. **Blazor Server (ADO.NET)** — an intermediate version using Blazor Server with ADO.NET

Both prototypes applied core OOP principles (abstract base classes, polymorphism, encapsulation) around `User` and `Property` entities in a government registry context. MirasPro is the current, more complete iteration, now built on Entity Framework Core.

## Roadmap

- [ ] Add unit tests
- [ ] Deploy to production environment
- [ ] Add multi-language support (Urdu/English)


