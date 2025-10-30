# Connect Four — Server (ASP.NET Core Razor Pages) + Client (WinForms)

A **Connect Four** solution with an ASP.NET Core (.NET 8) server using **Razor Pages + Web API** and a **WinForms** desktop client.  
The project uses **Entity Framework Core** (SQL Server **LocalDB**) with a ready‑to‑apply initial migration. The client draws the board, plays drop animations, restores games from history, and talks to the server via REST.

---

## 🚀 What’s Included
- **ServerApp** — ASP.NET Core Razor Pages site with pages for Login/Welcome/Games/Players, plus a REST controller (e.g., `ConnectFourApiController`) to manage games and moves.
- **ClientApp** — WinForms (.NET 8) desktop client that calls the API (default: `http://localhost:5136/api/ConnectFourApi/`), renders a 6×7 board, runs disk‑drop animations, shows turn/results, and can replay a game from its moves.
- **EF Core + SQL Server LocalDB** — Default connection to `ConnectFourDB` with an initial migration under `ServerApp/Migrations`.
- **Optional two‑way integration** — The server can launch the client if you set `ClientApp:CustomExePath` in `ServerApp/appsettings.json`.

## 🗂️ Solution Structure
```
ConnectFourSolution.sln
├─ ServerApp/                      # ASP.NET Core Razor Pages + Web API (.NET 8)
│  ├─ Data/                        # DbContext, seeding utilities (if present)
│  ├─ Migrations/                  # EF Core migrations (InitialCreate)
│  ├─ Models/                      # Entities: Player, Game, Move
│  ├─ Pages/                       # Razor Pages: Login, Welcome, Games, About, etc.
│  ├─ WebApiCoreManagement/        # ConnectFourApiController (REST endpoints)
│  ├─ appsettings.json             # ConnectionStrings + ClientApp.CustomExePath
│  └─ Properties/launchSettings.json  # Ports (e.g., http://localhost:5136)
└─ ClientApp/                      # WinForms client (.NET 8)
   ├─ Models/                      # Shared DTO/POCOs used by the client
   ├─ Form1.cs                     # Board drawing, animations, API calls
   └─ Program.cs                   # Entry point
```

## 🧰 Tech Stack
- **.NET 8**
- **ASP.NET Core Razor Pages**
- **Entity Framework Core 9** (SqlServer + Tools)
- **WinForms (.NET 8)**
- **Bootstrap** (light styling for server pages)

## ✅ Prerequisites
- **Visual Studio 2022 (latest)** or **.NET SDK 8+**
- **SQL Server Express LocalDB** (bundled with VS)
- (Optional) EF tools for CLI:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## ⚙️ Quick Configuration
Check `ServerApp/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ConnectFourDB;Trusted_Connection=True;"
  },
  "ClientApp": {
    "CustomExePath": "C:\\full\\path\\to\\ClientApp.exe"
  }
}
```
- **Ports**: `Properties/launchSettings.json` sets something like `http://localhost:5136`.  
  The client’s `HttpClient.BaseAddress` (in `Form1.cs`) must match the server port and base route.

## 🗄️ Initialize the Database
An initial migration already exists under `ServerApp/Migrations`. Apply it with:
```bash
cd ServerApp
dotnet ef database update
```
> Using Visual Studio? Open **Package Manager Console** → set **Default project** to `ServerApp` → run `Update-Database`.

## ▶️ Run with Visual Studio
1. Open `ConnectFourSolution.sln` in **Visual Studio 2022**.
2. Set **ServerApp** as the **Startup Project** and run (**F5**). The server should listen on `http://localhost:5136`.
3. Open the site home → **Login** (enter an existing Player ID or create one via the Players/Welcome pages, depending on the implementation).
4. Go to **Games** to create/delete/list games.
5. (Optional) Configure `ClientApp:CustomExePath` in `appsettings.json` to allow launching the client from the server.
6. You can also run the client manually from VS or from:
   `ClientApp/bin/Debug/net8.0-windows/ClientApp.exe`

## ▶️ Run from the Command Line
```bash
# Terminal 1 — Server
cd ServerApp
dotnet ef database update   # first time only
dotnet run                  # listens on http://localhost:5136

# Terminal 2 — Client (after building in VS or with dotnet build)
cd ClientApp/bin/Debug/net8.0-windows
./ClientApp.exe
```
> **Important**: The client expects the API at `http://localhost:5136/api/ConnectFourApi/` (as set in `Form1.cs`). If you change the port or route, update the client accordingly.

## 🧩 REST API (Summary)
The REST controller lives in `ServerApp/WebApiCoreManagement/ConnectFourApiController.cs`. Common patterns include:
- `POST  api/ConnectFourApi/newgame` → creates a new game and returns its `gameId`.
- `GET   api/ConnectFourApi/moves/{gameId}` → returns all moves for a game.
- `POST  api/ConnectFourApi/move` with body `{ gameId, playerId, row, column }` → submits a move if valid.
- `GET   api/ConnectFourApi/board/{gameId}` → returns the inferred board state from move history.
> Exact action names/routes depend on your controller code; adjust here if they differ.

## 🎮 Client Capabilities
- Renders a **6×7** board with a small HUD (current turn, result).
- **Disk drop animation** and **game replay** from move history.
- Prevents illegal moves (full column) and checks for game over.
- Buttons for **Restart**, **Load**, and selecting a **GameId** from a list.
- Uses `HttpClient` with a base URL matching the server.

## 🛠️ Useful Commands
```bash
# build & run server
dotnet build ServerApp
dotnet run --project ServerApp

# add a new EF migration (after model changes)
cd ServerApp
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## 🧪 Troubleshooting
- **Mismatched port** — If the server isn’t on `5136`, update `HttpClient.BaseAddress` in `ClientApp/Form1.cs` or change `applicationUrl` in `launchSettings.json`.
- **LocalDB missing** — Install SQL Server Express/LocalDB or point the connection string to a reachable SQL Server.
- **EF errors** — Ensure **Default project** in PMC is `ServerApp` and that `Migrations` live there.
- **Launching client from server** — Set `ClientApp:CustomExePath` to a valid `ClientApp.exe` path.

## 📄 License
Educational/academic project. Add your preferred license if you intend to distribute.

---

**Tech**: .NET 8 · Razor Pages · EF Core · WinForms
