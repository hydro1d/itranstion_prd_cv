# CV Management & Recruitment Platform

A modern, production-grade **CV Management and Recruitment Platform** built with **ASP.NET Core / Blazor Web App (.NET 9)**, **PostgreSQL + Entity Framework Core**, and **Bootstrap 5** featuring a custom developer/HR tool visual identity.

Designed specifically for **clean maintainability, explainability during live evaluation/defense, and robust candidate-recruiter workflows**.

---

## 🎯 Executive Summary & The Three Killer Features

Traditional job boards treat resumes as static PDF files that candidates manually rewrite for every job application. This platform solves that problem through a structured, data-driven approach:

1. **Reusable Attribute Library (Killer Feature #1)**:
   - Recruiters define standardized dynamic attributes (Text, Number, Date, Dropdown, Multi-select, URL, etc.) categorized systematically.
   - Candidates enter their qualifications once into their profile under these standardized attributes.
2. **Customizable Position Templates (Killer Feature #2)**:
   - Recruiters compose job openings by picking required and optional attributes from the library with custom ordering and validation constraints.
3. **Automatic CV Generation & Editor (Killer Feature #3)**:
   - When a candidate selects a position, the platform automatically maps their master profile attributes to the position requirements, detects any missing required fields in real time, and produces a tailored CV.
   - Supported by optimistic concurrency control (`RowVersion`) for auto-saving and a strict database constraint: **1 CV per candidate per position**.

---

## 🏗️ Architectural Decisions & Defense Justifications

### 1. Why Blazor Web App (Interactive Server)?
- **Evaluation Defense Point**: Blazor provides rich, single-page-app interactivity (real-time field validation, auto-save debouncing, inline CV editing, dynamic row selection toolbars) entirely in C# without duplicating domain models or validation logic between client and server.
- **Server Interactivity**: Server-side interactivity avoids heavy WebAssembly bundle downloads, provides instant startup, and keeps database connection logic and secrets securely on the server.

### 2. Why PostgreSQL?
- **Evaluation Defense Point**: PostgreSQL is a robust, open-source relational database that excels at JSON/dynamic attribute queries, full-text search indexing, and enforcing strict relational integrity (composite unique keys like `CandidateId + PositionId` and `RecruiterId + CVId`).

### 3. Why Entity Framework Core?
- **Evaluation Defense Point**: EF Core provides strongly typed LINQ queries, automatic change tracking, and transparent migrations. We strictly avoid over-abstracting with generic repositories: EF Core's `DbContext` and `DbSet` are already an implementation of Unit of Work and Repository. Read operations strictly employ `AsNoTracking()` with selective projections to prevent N+1 queries and memory bloat.

### 4. Why Bootstrap 5 + Bespoke Design System?
- **Evaluation Defense Point**: Bootstrap 5 provides battle-tested, accessible layout grids and utility primitives without heavy JavaScript dependencies. Instead of looking like a generic template, it is extensively customized with custom CSS design tokens (slate/zinc neutral surfaces, precision cobalt accents, flat panels, and compact headers) completely eliminating generic AI-generated aesthetics.

---

## 🎨 UI/UX Design System Highlights

- **Anti-AI Dashboard Aesthetic**: Zero cheesy purple gradients, zero excessive drop shadows, no generic rounded cards. Flat surfaces, clean borders, crisp typography (`Inter`), and high-density information display.
- **Native Dual Theme**:
  - **Light Theme**: High-contrast, crisp white and slate background (`#f8fafc`).
  - **Dark Theme**: Deep slate/navy surfaces (`#090d16` / `#111827`) calibrated for long-session readability.
  - **Persistence**: Instantly applied via client-side script before first paint to prevent Flash of Unstyled Content (FOUC).
- **Table-Centric Action Toolbar**:
  - In compliance with evaluation requirements, **no repeating action buttons clutter individual table rows**.
  - Selecting a row highlights it and illuminates contextual toolbar actions (View Details, Generate CV, Filter, etc.).
- **Multi-Lingual Localization**:
  - First-class support for **English (`en`)** and **Bangla (`bn`)** via ASP.NET Core `.resx` resources and culture cookie persistence.

---

## 🌿 Incremental Git Branching Roadmap

Development proceeds incrementally one feature branch at a time, each verified, tested, and merged into `main`:

```text
main
│
├── feature/project-setup           (scaffolding, design tokens, layout, localization foundation)
├── feature/database-foundation      (core entities, EF Core DbContext, PostgreSQL migrations)
├── feature/authentication           (Identity, roles: Candidate, Recruiter, Admin, social login)
├── feature/attribute-library        (attribute categories, types, dropdown options, table UI)
├── feature/position-management      (position CRUD, builder, required/optional attributes)
├── feature/candidate-profile        (4 sections: Me, Info, Projects with Markdown, CVs, auto-save)
├── feature/cv-generation            (auto-mapping engine, missing field detection, 1 CV per position)
├── feature/cv-editor                (document CV editor, missing info panel, completion %, concurrency)
├── feature/recruiter-cv-management  (recruiter table, read-only preview, likes with unique constraint)
├── feature/discussions              (SignalR real-time position discussion messaging)
├── feature/search-filtering         (PostgreSQL full-text indexes, pagination, query optimization)
├── feature/localization-theme       (bilingual dictionary expansion, theme polish)
└── feature/ui-polish                (mobile responsiveness, empty states, toasts, defense polish)
```

---

## 🚀 Getting Started Locally

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 14+](https://www.postgresql.org/) (Running on `localhost:5432` or via Docker)

### 1. Clone & Setup
```bash
git clone https://github.com/hydro1d/itranstion_prd_cv.git
cd itranstion_prd_cv
```

### 2. Configuration
Verify `src/CvPlatform/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=cv_platform_db;Username=postgres;Password=postgres"
  }
}
```

### 3. Restore & Build
```bash
dotnet restore
dotnet build
```

### 4. Run the Application
```bash
dotnet run --project src/CvPlatform
```
Navigate to `https://localhost:5001` or `http://localhost:5000` in your web browser.

---

## 👥 Roles and Permissions Matrix

| Capability | Public | Candidate | Recruiter | Administrator |
|---|:---:|:---:|:---:|:---:|
| Browse & Search Public Positions | ✅ | ✅ | ✅ | ✅ |
| View Platform Metrics | ✅ | ✅ | ✅ | ✅ |
| Build Reusable Profile & Projects | ❌ | ✅ | ❌ | ✅ |
| Auto-Generate & Edit CVs | ❌ | ✅ | ❌ | ✅ (All) |
| Create Reusable Attribute Library | ❌ | ❌ | ✅ | ✅ |
| Create & Publish Positions | ❌ | ❌ | ✅ | ✅ |
| Review Candidate CVs & Like | ❌ | ❌ | ✅ | ✅ |
| Position Discussions | ❌ | ✅ | ✅ | ✅ |
| User & Role Management | ❌ | ❌ | ❌ | ✅ |

---

## 📜 Evaluation Defense Talking Points

1. **Why avoid generic repositories?**
   - EF Core's `DbContext` is already a Unit of Work, and `DbSet<T>` is a Repository. Adding a generic repository often strips EF Core's best features (projections with `.Select()`, batch operations, async streaming) and leads to N+1 query traps.
2. **How is CV Generation uniquely constrained?**
   - The database enforces a compound unique index on `(CandidateId, PositionId)`. A candidate cannot create accidental duplicate CVs for the same opening; subsequent actions open and update the existing tailored document.
3. **How is data loss prevented during inline editing?**
   - The CV entity uses an optimistic concurrency token (`RowVersion` / `xmin` in PostgreSQL) combined with a debounced auto-save handler. If another session edits the record concurrently, a `DbUpdateConcurrencyException` is caught gracefully.
