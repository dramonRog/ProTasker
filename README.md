# ProTasker (Backend API)
> A scalable backend architecture for a comprehensive project and task management system, designed to support Kanban-style workflows and team collaboration.

## Table of Contents
* [General Information](#general-information)
* [Technologies Used](#technologies-used)
* [Features](#features)
* [Architecture & Patterns](#architecture--patterns)
* [Setup](#setup)
* [Usage](#usage)
* [Project Status](#project-status)
* [Room for Improvement](#room-for-improvement)
* [Contact](#contact)

## General Information
- **ProTasker** is a robust RESTful API built to power a SaaS-like project management tool (similar to Trello or Jira). 
- The main purpose of this application is to provide a reliable and secure foundation for managing projects, organizing tasks into customizable boards, and facilitating team collaboration through comments and role-based access control (RBAC).
- I developed this project independently to deepen my expertise in backend engineering, focusing on clean N-tier architecture, advanced Entity Framework Core capabilities (like Soft Deletion), and secure API design.

## Technologies Used
- **Framework:** .NET 10.0 Web API
- **ORM:** Entity Framework Core 8.0
- **Database:** PostgreSQL
- **Authentication:** JWT (JSON Web Tokens)
- **Object Mapping:** AutoMapper
- **Validation:** FluentValidation

## Features
Ready features include:
- **Secure Authentication & Authorization:** User registration, login via JWT, and Role-Based Access Control (RBAC) to ensure users only see and modify projects they are assigned to.
- **Project & Board Management:** Create and manage projects, configure Kanban-style boards, and implement custom board reordering logic.
- **Advanced Task Management:** Assign tasks to specific project members, set task priorities, and smoothly move tasks across different boards.
- **Team Collaboration:** Ability to add/update comments on individual task items.
- **Data Integrity:** Implementation of "Soft Deletion" across core entities to prevent accidental data loss and preserve historical records.
- **Optimized Performance:** Built-in flexible pagination for retrieving large sets of data efficiently.

## Architecture & Patterns
This project strictly adheres to clean code principles and modern .NET development standards:
- **N-Tier Architecture:** Clear separation of concerns between `Controllers` (HTTP routing), `Services` (Business Logic), and `Data Access` (EF Core).
- **DTO Pattern:** Utilization of Data Transfer Objects and `AutoMapper` to decouple internal database entities from external API contracts.
- **Validation Pipeline:** Robust request validation using `FluentValidation` to ensure data consistency before it hits the service layer.
- **Centralized Error Handling:** A custom `GlobalExceptionHandler` middleware to catch exceptions and return standardized, secure error responses to the client.

## Setup

### Required Tools
* [Visual Studio 2026](https://visualstudio.microsoft.com/downloads/) or Rider
* [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
* [pgAdmin](https://www.pgadmin.org/download/) (LocalDB, Express, or Developer Edition)
  
### Local Manual Setup

#### 1. Clone the Repository
```bash
git clone https://github.com/dramonrog/ProTasker.git
cd ProTasker/ProTasker
```

#### 2. Configure the Database
Open `appsettings.json` or `appsettings.Development.json` and locate the `ConnectionStrings` section. Update it to match your local SQL Server instance.
*Example for SQL LocalDB:*

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProTaskerDB;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```
#### 3. Apply Database Migrations
Ensure the database is created and the schema is applied using Entity Framework Core tools:
```bash
dotnet ef database update
```

> **💡 Troubleshooting `dotnet ef`:**
> - If you get a "Command not found" error, install the tools globally: `dotnet tool install --global dotnet-ef`.

#### 4. Run the Backend
```bash
dotnet run
```

The API will start locally. You can access the **Swagger UI** to explore and test the endpoints (usually at `https://localhost:<port>/swagger`).

## Usage

### Testing Endpoints via Swagger
1. Navigate to the `/swagger` endpoint in your browser after launching the application.
2. **Register/Login:** Use the `Auth` endpoints to create a new user and generate a JWT token.
3. **Authorize:** Copy the generated token, click the "Authorize" button at the top of the Swagger UI, and enter: `Bearer <your_token>`.
4. **Explore:** You can now interact with protected endpoints (`Projects`, `Boards`, `TaskItems`, `ProjectMembers`, `TaskComments`).

### Core Workflows
- **Admin/Owner:** The creator of a project automatically gets Owner permissions, allowing them to manage `ProjectMembers` and change their roles.
- **Task Management:** Users can fetch tasks using flexible query parameters (pagination) and update their statuses by moving them across specific boards.

## Project Status
Project is: _in progress_.  
The core backend architecture, database schema, and essential business logic are fully implemented and stable. 

## Room for Improvement
- **Frontend Integration:** Develop a modern SPA (e.g., Angular or React) to consume the API and provide a visual Kanban board interface.
- **Unit & Integration Testing:** Expand test coverage for core services using xUnit and Moq.
- **Caching Layer:** Implement Redis distributed caching for frequently accessed data (like board layouts) to improve performance.
- **Real-time Updates:** Integrate SignalR for real-time board updates across connected clients.

## Contact
Created by [@dramonrog](https://github.com/dramonrog) - feel free to contact me!
