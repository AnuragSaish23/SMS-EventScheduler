# SMS Event Scheduler — Backend API

A .NET 8 backend that powers the **SMS CMMS** (Computerized Maintenance Management System) for industrial signal processing and automated delay classification.

## What This Does

- **Receives factory signals** from PLCs via REST API
- **Automatically starts/stops timers** based on signal configuration
- **Auto-classifies delays** (e.g., Pump Failure, Die Change) using a 3-level classification tree
- **Serves data** to the Grafana frontend plugin for live dashboard visualization

## Architecture

```
Factory PLC → POST /api/signals → SignalProcessingEngine (Singleton)
                                        ↓
                                  Timer starts/stops
                                        ↓
                                Classification auto-assigned
                                        ↓
                                  Saved to PostgreSQL
                                        ↓
                          GET /api/events ← Grafana frontend
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/signals` | Receive raw signals from PLC |
| `GET` | `/api/events` | Get all timing events with classifications |
| `GET` | `/api/events/active` | Check if a timer is currently running |
| `GET` | `/api/config` | Get all signal configurations |
| `POST` | `/api/config` | Create a signal configuration |
| `GET` | `/api/classification/tree` | Get the full classification tree |
| `POST` | `/api/classification` | Create a classification node |
| `DELETE` | `/api/classification/{id}` | Delete a classification (cascading) |

## Prerequisites

Before running this project, make sure you have:

1. **.NET 8 SDK** — [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **PostgreSQL** — [Download here](https://www.postgresql.org/download/)
3. **pgAdmin** (optional, for viewing the database)

## Setup Instructions

### 1. Clone this repository
```bash
git clone https://github.com/YOUR_USERNAME/SMS-EventScheduler.git
cd SMS-EventScheduler
```

### 2. Create the PostgreSQL database
Open **pgAdmin** (or psql) and create a new database:
```sql
CREATE DATABASE "EventSchedulerDb";
```

### 3. Update the connection string
Open `EventScheduler/appsettings.json` and update with your PostgreSQL credentials:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=EventSchedulerDb;Username=postgres;Password=YOUR_PASSWORD"
  }
}
```

### 4. Run database migrations
```bash
cd EventScheduler
dotnet ef database update
```

> **Note:** If you don't have the EF tool, install it first:
> ```bash
> dotnet tool install --global dotnet-ef
> ```

### 5. Run the API
```bash
dotnet run
```

The API will start on `http://localhost:5032`.

### 6. Verify it works
Open your browser or Postman and visit:
```
http://localhost:5032/api/events
```
You should see an empty array `[]` — that means it's working!

## Connecting to the Grafana Frontend

1. Make sure this backend is running on `http://localhost:5032`
2. Start the Grafana plugin (`npm run server` in sms-demo-app)
3. The frontend fetches data from this API automatically
4. CORS is pre-configured to allow requests from `http://localhost:3000`

## Project Structure

```
SMS_EventScheduler/
├── EventScheduler/
│   ├── Controllers/         # API endpoints
│   │   ├── SignalController.cs
│   │   ├── TimingEventController.cs
│   │   ├── SignalConfigController.cs
│   │   └── ClassificationController.cs
│   ├── Services/            # Business logic
│   │   ├── SignalProcessingEngine.cs   # Singleton timer engine
│   │   └── TimingEventService.cs       # Database operations
│   ├── Models/              # Database entities
│   ├── DTOs/                # Request/Response models
│   ├── Data/                # DbContext
│   ├── Migrations/          # EF Core migrations
│   ├── Program.cs           # App startup & CORS config
│   └── appsettings.json     # Connection string
└── EventScheduler.slnx      # Solution file
```

## Built By

**Anurag Drumagar** — Backend Developer Intern @ SMS Group
