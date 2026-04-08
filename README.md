# SmartTaskManager

SmartTaskManager is a `.NET 10` C# console application for managing users and tasks through a layered, Clean Architecture-based design.

The project demonstrates how to build a structured task management system with clear separation of concerns, object-oriented design, and a beginner-friendly console interface. It is useful as both a learning project and a portfolio project because it combines practical business features with maintainable architecture.

## Features

- Create and manage users
- Create personal, work, and learning tasks
- Track task priority and status
- Mark tasks as completed
- Archive tasks without deleting history
- View task history for lifecycle changes
- Filter tasks by status
- Filter tasks by priority
- View overdue tasks
- Use a console-based dashboard summary
- Work with seeded demo data for fast exploration
- Store data in memory for a simple local setup

## Architecture Overview

The solution follows a simple Clean Architecture approach with four main layers:

### Domain

The Domain layer contains the core business model and rules.

- Entities such as `User`, `BaseTask`, `PersonalTask`, `WorkTask`, and `LearningTask`
- Enums for task priority and task status
- History tracking records
- Repository and notification contracts used by higher layers

### Application

The Application layer coordinates use cases.

- Task and user services
- Task filtering logic
- DTOs used by the UI
- Workflow orchestration without storage or console concerns

### Infrastructure

The Infrastructure layer provides technical implementations.

- In-memory repositories for users and tasks
- Console notification service
- Time provider implementation

### UI

The UI layer provides the console experience.

- Menu navigation
- Dashboard summary
- Table-style output rendering
- Input validation and user guidance

## Technologies Used

- C#
- .NET 10
- Console Application
- Object-Oriented Programming
- Clean Architecture

## How to Run the Project

1. Clone or download the repository.
2. Open a terminal in the project root.
3. Restore dependencies:

```powershell
dotnet restore .\SmartTaskManager.sln
```

4. Build the solution:

```powershell
dotnet build .\SmartTaskManager.sln
```

5. Run the console application:

```powershell
dotnet run --project .\src\SmartTaskManager.UI.Console\SmartTaskManager.UI.Console.csproj
```

If needed, use this alternative build flow:

```powershell
dotnet msbuild .\SmartTaskManager.sln /t:Restore /m:1
dotnet msbuild .\SmartTaskManager.sln /t:Build /m:1
dotnet run --project .\src\SmartTaskManager.UI.Console\SmartTaskManager.UI.Console.csproj --no-build --no-restore
```

## Example Usage

When the application starts, the user is presented with a dashboard and a grouped menu.

Typical flow:

1. Review the dashboard summary.
2. List users or select an active user.
3. Create a new task for that user.
4. View tasks in a structured table.
5. Update task priority, complete tasks, or archive them.
6. Filter tasks by status or priority.
7. View overdue tasks and inspect task history.

The application also starts with sample users and tasks so the features can be explored immediately.

## Project Structure

```text
SmartTaskManager/
├─ SmartTaskManager.sln
├─ README.md
├─ docs/
│  └─ architecture.md
├─ src/
│  ├─ SmartTaskManager.Domain/
│  ├─ SmartTaskManager.Application/
│  ├─ SmartTaskManager.Infrastructure/
│  └─ SmartTaskManager.UI.Console/
└─ tests/
   ├─ SmartTaskManager.Domain.Tests/
   ├─ SmartTaskManager.Application.Tests/
   └─ SmartTaskManager.Infrastructure.Tests/
```

## Learning Goals

This project demonstrates:

- Object-oriented design with encapsulation, inheritance, and polymorphism
- Clean Architecture and separation of concerns
- Service and repository patterns
- Console UI design and user flow improvements
- AI-assisted development used to plan, implement, review, and refine the project

## Future Improvements

- Replace in-memory storage with `EF Core` and `SQLite` or `SQL Server`
- Add a web API or ASP.NET Core frontend
- Build a desktop UI with `WPF`, `WinUI`, or `Blazor Hybrid`

## Author

**Name:** _Your Name Here_

**GitHub:** _Your GitHub Profile_

**LinkedIn:** _Your LinkedIn Profile_
