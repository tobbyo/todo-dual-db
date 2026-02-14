# CLAUDE.md

## Project Overview
Todo list app with .NET Minimal API backend and Vue 3 frontend.

## Tech Stack
- Backend: .NET Minimal API, EF Core, SQLite, MongoDB.Driver
- Frontend: Vue 3
- Databases:
  - SQLite (`/api/todos.db`) — structured todo data
  - MongoDB (`todo_app` / `todo_app_dev`) — activity log documents (schema-flexible)
- Architecture: Polyglot persistence — SQLite for relational data, MongoDB for flexible document storage

## Project Structure
- `/api` - .NET backend
  - `/api/Models` - Domain models and DTOs
  - `/api/Data` - EF Core DbContext
  - `/api/Services` - Business logic (activity logging)
  - `/api/Endpoints` - API endpoint definitions
- `/client` - Vue 3 frontend

## Key Commands
- `docker run -d --name todo-mongo -p 27017:27017 mongo:7` - Start MongoDB (required)
- `cd api && dotnet run` - Start backend
- `cd client && npm run dev` - Start frontend
- `cd api && dotnet ef migrations add <name>` - Create migration
- `cd api && dotnet ef database update` - Apply migrations
- `cd api.tests && dotnet test` - Run backend tests
- `cd client && npm test` - Run frontend tests

## Coding Standards
- Use minimal API style (no controllers)
- Use async/await on all DB calls
- Keep endpoints in logically grouped files, not all in Program.cs
- Use record types for DTOs
- Frontend components in SFCs with `<script setup>` syntax

## Current State
- Basic CRUD for todos is working
- SQLite database with single Todos table
- MongoDB activity log tracks all CRUD operations with schema-flexible documents
- Activity log panel in frontend with collapsible timeline view

## Known Issues
- (add as they come up)

## Possible Features

  Quick wins
  - Due dates — date picker + overdue highlighting
  - Priority levels — low/medium/high with color-coded badges
  - Search/filter — text search + filter by status (all/active/completed)
  - Bulk actions — select multiple, mark complete, or delete
  - Character counts/validation — title max length, required field feedback

  Intermediate
  - Categories/tags — label todos with colored tags, filter by tag
  - Drag-and-drop reorder — manual ordering with a library like vuedraggable
  - Dark mode — toggle with Tailwind's dark variant, persist preference
  - Undo delete — soft delete with a toast "Undo" action
  - Subtasks/checklists — nested checklist items within a todo

  More ambitious
  - User auth — ASP.NET Identity or JWT, per-user todo lists
  - Real-time sync — SignalR so multiple tabs/devices stay in sync
  - Recurring todos — daily/weekly/monthly repeating tasks
  - File attachments — upload images or files to a todo
  - ~~Activity log/history~~ — DONE (MongoDB-backed, see ActivityLog component)
  - Export — download todos as CSV/JSON
  - PWA support — offline access + installable on mobile
  - Dashboard stats — completion rate, todos per day chart, streaks