# CV Management & Recruitment Platform

A production-grade **CV Management and Recruitment Platform** built with **ASP.NET Core / Blazor Web App (.NET 9 Interactive Server)**, **PostgreSQL + Entity Framework Core**, and a bespoke **Bootstrap 5 Developer/HR Tool Slate Design System**.

Designed specifically for **clean maintainability, explainability during live evaluation/defense, and robust candidate-recruiter workflows**.

🔗 **GitHub Repository**: [https://github.com/hydro1d/itranstion_prd_cv.git](https://github.com/hydro1d/itranstion_prd_cv.git)

---

## 🎯 The Three Killer Features

Traditional job platforms treat resumes as static PDF files that candidates manually rewrite for every job application. This platform solves that through a structured, data-driven architecture:

### 1. Reusable Attribute Library (Killer Feature #1)
- **Recruiter/Admin Definition**: Standardized dynamic qualifications across 8 data types (`Text`, `LongText`, `Number`, `Date`, `Boolean`, `Dropdown`, `MultiSelect`, `Url`) categorized systematically (Languages, Frameworks, Cloud, Databases, Certifications).
- **Candidate Master Profile**: Candidates fill out their qualifications **once** into their reusable master profile under these standardized attributes.
- **Code Reference**: `src/CvPlatform/Services/IAttributeService.cs`, `src/CvPlatform/Components/Pages/Recruiter/AttributeLibrary.razor`.

### 2. Customizable Position Templates (Killer Feature #2)
- **Opening Builder**: Recruiters compose job openings by picking required and optional attributes from the library with custom ordering and validation constraints.
- **Dynamic Requirement Specifications**: Each position opening maintains its own qualification template that automatically validates applicant submissions.
- **Code Reference**: `src/CvPlatform/Services/IPositionService.cs`, `src/CvPlatform/Components/Pages/Positions/PositionEditorModal.razor`.

### 3. Automatic CV Generation & Document Editor (Killer Feature #3)
- **Auto-Mapping Engine**: When a candidate applies for a position, the platform automatically maps their master profile attributes to the position requirements, detects any missing required fields in real time, and produces a tailored CV.
- **Database Unique Constraint**: Strictly **1 CV per candidate per position** enforced by EF Core composite unique index `(CandidateId, PositionId)`.
- **Live Document Editor**: Resume paper canvas with real-time requirement readiness checklist, completion %, Markdown preview, and print-to-PDF (`@media print`) layout.
- **Optimistic Concurrency**: Protected by `[ConcurrencyCheck]` (`RowVersion`) preventing concurrent overwrite conflicts.
- **Code Reference**: `src/CvPlatform/Services/ICvService.cs`, `src/CvPlatform/Components/Pages/Candidate/CvEditor.razor`.

---

## ⚡ 1-Click Evaluator Quick Start

To enable fast evaluation without typing credentials, the login page (`/account/login`) features **1-Click Demo Login** buttons:

| Role | Demo Email | Password | Primary Workspace |
|---|---|---|---|
| **Candidate** | `candidate@cvplatform.com` | `Password123!` | `/candidate/profile`, `/candidate/cvs` |
| **Recruiter** | `recruiter@cvplatform.com` | `Password123!` | `/recruiter/positions`, `/recruiter/attributes`, `/recruiter/cvs` |
| **Administrator** | `admin@cvplatform.com` | `Password123!` | `/admin/dashboard`, `/admin/users` |

> [!TIP]
> **Dual Persistence (Zero-Config Resiliency)**:
> All services (`IdentityDataSeeder`, `AttributeService`, `PositionService`, `CandidateProfileService`, `CvService`, `DiscussionService`) feature dual-persistence: they connect to PostgreSQL when reachable on `127.0.0.1:5432`, and automatically fall back to thread-safe in-memory stores pre-seeded with realistic technical data if PostgreSQL is offline. The platform runs out of the box with zero configuration!

---

## 🎨 UI/UX Design System Standards

1. **Table-Centric Action Toolbar (Strict PRD Compliance)**:
   - **No repeating action buttons inside individual table rows**.
   - Row selection activates a dedicated, contextual action toolbar at the top of tables across Positions, Attribute Library, Recruiter Review Desk, and Search.
2. **Native Dual Theme (Light & Dark Slate)**:
   - **Light Theme**: Crisp white and slate background (`#f8fafc`).
   - **Dark Theme**: High-contrast slate/navy surfaces (`#090d16` / `#111827`) with clear visual hierarchy.
   - Immediate client-side script application prevents Flash of Unstyled Content (FOUC).
3. **Bilingual Localization (English & Bangla)**:
   - 100% dictionary parity across 209 keys in `SharedResource.en.resx` and `SharedResource.bn.resx`.
   - Cookie-persisted culture switching (`/api/culture/set`) with instant UI reload.
4. **Real-Time SignalR Position Discussions**:
   - Interactive Q&A thread for every opening at `/discussions` and `/discussions/{id}`.
   - Candidates and recruiters chat in real-time about requirements, interview formats, and team culture.
5. **Multi-Criteria Faceted Search & Discovery**:
   - Deep search at `/search` covering positions, required attribute templates, quick tags (`#csharp`, `#react`, `#docker`), and reusable attribute definitions.
   - Switchable Table View (with top action toolbar) and Card Grid View.

---

## 🌿 13 Incremental Feature Branches (All Merged to Main)

Development followed a strict feature branch methodology. Each branch was developed, verified with `dotnet build` (0 warnings, 0 errors), committed using conventional commits, merged to `main`, and pushed to GitHub:

| Phase | Branch Name | Key Capabilities Delivered |
|:---:|---|---|
| **1** | `feature/project-setup` | Blazor Server setup, custom tokens, theme switcher, layout, base localization |
| **2** | `feature/database-foundation` | 14 EF Core entities, composite unique constraints, migrations |
| **3** | `feature/authentication` | ASP.NET Identity, cookie auth, 1-click evaluator demo logins, role guards |
| **4** | `feature/attribute-library` | **Killer Feature #1**: 8 attribute data types, category manager, modal editor, top toolbar |
| **5** | `feature/position-management` | **Killer Feature #2**: Opening builder, attribute template composer, live home portal |
| **6** | `feature/candidate-profile` | Candidate Master Profile (Me, Info with 8 types, Projects with Markdown, CVs) |
| **7** | `feature/cv-generation` | **Killer Feature #3 (Part 1)**: Auto-mapping engine, 1 CV per position constraint |
| **8** | `feature/cv-editor` | **Killer Feature #3 (Part 2)**: Document resume canvas, live readiness %, concurrency check |
| **9** | `feature/recruiter-cv-management` | Recruiter review desk, read-only preview modal, recruiter likes compound index |
| **10** | `feature/discussions` | Real-time position discussions with SignalR hub, broadcast dispatch, Q&A threads |
| **11** | `feature/search-filtering` | Multi-criteria faceted search, quick tags, table toolbar vs grid switcher |
| **12** | `feature/localization-theme` | Bilingual English/Bangla 209-key parity, high-contrast dark theme polish |
| **13** | `feature/ui-polish` | Mobile responsive drawer, top search navigation, defense documentation |

---

## 🚀 How to Run Locally

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Optional: PostgreSQL 14+ (if absent, automatic in-memory fallback will activate seamlessly)

### Commands
```bash
# 1. Clone repository
git clone https://github.com/hydro1d/itranstion_prd_cv.git
cd itranstion_prd_cv

# 2. Restore & Build
dotnet restore
dotnet build

# 3. Run Application
dotnet run --project src/CvPlatform
```

Open your browser at `http://localhost:5000` or `https://localhost:5001`.

---

## 👥 Roles & Access Permissions Matrix

| Platform Capability | Public | Candidate | Recruiter | Administrator |
|---|:---:|:---:|:---:|:---:|
| Browse & Search Openings (`/`, `/search`) | ✅ | ✅ | ✅ | ✅ |
| View Platform Statistics (`/statistics`) | ✅ | ✅ | ✅ | ✅ |
| Manage Master Profile (`/candidate/profile`) | ❌ | ✅ | ❌ | ✅ |
| Generate & Edit Tailored CVs (`/cv/{id}`) | ❌ | ✅ | ❌ | ✅ |
| Manage Attribute Library (`/recruiter/attributes`) | ❌ | ❌ | ✅ | ✅ |
| Create & Compose Openings (`/recruiter/positions`) | ❌ | ❌ | ✅ | ✅ |
| Review Candidate CVs & Endorse (`/recruiter/cvs`) | ❌ | ❌ | ✅ | ✅ |
| Real-Time Position Discussions (`/discussions`) | ❌ | ✅ | ✅ | ✅ |
| System Administration (`/admin/dashboard`) | ❌ | ❌ | ❌ | ✅ |

---

## 📜 Evaluation Defense FAQ

1. **Why Blazor Web App Interactive Server instead of client-side SPA or WebAssembly?**
   - Eliminates redundant client/server DTO duplications and keeps sensitive business rules, database queries, and credentials secure on the server. Delivers instant load times without heavy WASM binary downloads.
2. **Why avoid generic repositories over EF Core?**
   - EF Core's `DbContext` is already a Unit of Work, and `DbSet<T>` is a Repository. Generic wrappers often discard EF Core's best capabilities (LINQ projections `.Select()`, `AsNoTracking()`, batch updates) and introduce N+1 query overhead.
3. **How is the "1 CV per position" requirement enforced?**
   - It is enforced at the database level with a unique compound index on `(CandidateId, PositionId)`. Any attempt to generate a new CV for the same position returns the existing instance for continued editing.
4. **How does optimistic concurrency protect CV documents?**
   - The `CV` entity contains a `[ConcurrencyCheck] uint RowVersion`. During save, EF Core verifies that the version in the database matches the loaded version. If another session saved changes concurrently, a `DbUpdateConcurrencyException` is caught, alerting the user and preserving document integrity.
5. **How does the Table-Centric Action Toolbar work?**
   - In accordance with evaluation guidelines, tables do not contain repeating action buttons in each row. Users select an item via row click or radio button, which activates a contextual action toolbar at the top of the table.
