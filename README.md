# WorkFlow — Workflow and Approval Management System

**English** | [Türkçe](README.tr.md)

A web application built with ASP.NET Core MVC that brings internal request/approval
processes, tasks, inventory and staff management together in one place. The user
interface is in Turkish.

## Features

- **Role-based authorization** — Admin, Manager and Staff roles (ASP.NET Core
  Identity, with Turkish error messages)
- **Request and approval flow** — create a request, send it to a manager for
  approval, approve / reject / request revision, bulk approval, action history (log)
- **Task management** — task assignment and tracking, calendar view
- **Inventory management** — asset registration, assigning items to staff,
  assignment history, bulk import from Excel
- **Staff registration** — sign-up with CV upload, account activation after manager approval
- **Real-time notifications** — SignalR
- **Chatbot** — a rule-based assistant that answers questions using system data
- **E-mail notifications** — SMTP
- **Background cleanup service** — automatic removal of old records
- **REST API** — workflow endpoints documented with Swagger

## Tech Stack

ASP.NET Core (.NET 10) MVC · Entity Framework Core · SQL Server · ASP.NET Core
Identity · SignalR · Swagger · Bootstrap · Docker

## Running

### With Docker

```bash
cp .env.example .env      # set SA_PASSWORD
docker compose up --build
```

The app opens at `http://localhost:5000`.

### Locally

The connection string in `appsettings.json` uses SQL Server LocalDB by default.

```bash
dotnet ef database update
dotnet run
```

To send e-mails, enter your own SMTP settings in the `EmailSettings` section of
`appsettings.json` (use an app password for Gmail).

## Project Structure

```
Controllers/   MVC and API controllers
Data/          DbContext and seed data (roles, default users)
Hubs/          SignalR hub
Models/        Entities and view models
Services/      E-mail, notification, chatbot, background services
Views/         Razor views
Migrations/    EF Core migrations
```
