# SMS Event Scheduler — Backend API

A .NET 10 backend that powers the **SMS CMMS** (Computerized Maintenance Management System) for industrial signal processing, automated delay classification, and real-time factory monitoring.

## What This Does

- **Receives factory signals** via OPC-UA protocol from an integrated factory simulator
- **Automatically starts/stops timers** based on signal configuration (StartTrigger / EndTrigger)
- **Auto-classifies delays** (e.g., Mechanical Delay, Setup Delay) using a 3-level hierarchical classification tree
- **Dual-writes telemetry** to both PostgreSQL (relational) and InfluxDB (time-series)
- **Serves data** to the Grafana frontend plugin for live dashboard visualization
- **Auto-seeds** the database with default classifications and signal configs on startup

## Architecture

```
OPC-UA Simulator (Node.js, port 4840)
    │ Toggles PumpSensor / SetupSensor / OperationComplete
    │ via OPC-UA protocol
    ▼
OpcUaClientService (BackgroundService)
    │ Subscribes to tag changes, maps to Signal.A / B / C
    ▼
SignalProcessingEngine (Singleton)  ◄── Also accepts REST POST /api/signals
    │ 1. Logs raw signal → PostgreSQL + InfluxDB
    │ 2. Checks signal type (Start/End Trigger)
    │ 3. Manages timer state machine
    │ 4. On EndTrigger: saves TimingEvent → PostgreSQL + InfluxDB
    ▼
PostgreSQL (port 5432)  +  InfluxDB (port 8086)
    │
    ▼
REST API (port 5032)
    │ GET /api/events, /api/classification/tree, /api/config
    ▼
Grafana Plugin (port 3000)
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/signals` | Receive raw signals (REST alternative to OPC-UA) |
| `GET` | `/api/events` | Get timing events (supports `from`, `to`, `page`, `pageSize`) |
| `GET` | `/api/events/active` | Check if a timer is currently running |
| `GET` | `/api/config` | Get all signal configurations |
| `POST` | `/api/config` | Create a signal configuration |
| `DELETE` | `/api/config/{id}` | Delete a signal configuration |
| `GET` | `/api/classification` | Get flat list of classifications |
| `GET` | `/api/classification/tree` | Get nested classification tree |
| `POST` | `/api/classification` | Create a classification node |
| `PUT` | `/api/classification/{id}` | Rename a classification |
| `DELETE` | `/api/classification/{id}` | Delete a classification (cascading) |

## Prerequisites

You only need **one thing** installed:

- **Docker Desktop** — [Download here](https://www.docker.com/products/docker-desktop/)

That's it! Docker handles PostgreSQL, InfluxDB, Node.js, and .NET automatically.

## Quick Start (Clone & Run)

### 1. Clone this repository
```bash
git clone https://github.com/AnuragSaish23/SMS-EventScheduler.git
cd SMS-EventScheduler
```

### 2. Start everything with Docker
```bash
docker-compose up --build
```

This single command will:
- Pull and start **PostgreSQL 16** (port 5432)
- Pull and start **InfluxDB 2.7** (port 8086) with pre-configured org, bucket, and token
- Build and start the **OPC-UA Simulator** (port 4840) — signals fire every 3-6 seconds
- Build and start the **.NET Backend API** (port 5032)
- Auto-create all database tables
- Auto-seed classifications ("Mechanical Delay", "Setup Delay") and signal configs

### 3. Verify it works

Wait ~15 seconds for the first signal cycle, then check:

| Service | URL | What to expect |
|---------|-----|----------------|
| Backend API | http://localhost:5032/swagger | Swagger UI with all endpoints |
| Events | http://localhost:5032/api/events | JSON array of timing events |
| InfluxDB | http://localhost:8086 | Dashboard (login: `admin` / `adminpassword`) |

You should see logs in the terminal like:
```
opcua-simulator-1  | [10:30:15 AM] 🔴 PUMP BROKE! (PumpSensor = true)
backend-1          | Timer STARTED by signal 'Signal.A'
opcua-simulator-1  | [10:30:19 AM] ⚡ OPERATION COMPLETE signal (OperationComplete = true)
backend-1          | Timer ENDED by signal 'Signal.C'. Duration: 00:00:04.002
backend-1          | InfluxDB: wrote timing event. Duration=00:00:04.002
```

### 4. Connect the Grafana Frontend (optional)

If you also have the [sms-project](https://github.com/khushimittal0/sms-project) frontend:

```bash
# Terminal 2
cd sms-project/sms-demo-app
npm install
npm run dev

# Terminal 3
npm run server
```

Then open http://localhost:3000 for the Grafana dashboards.

### 5. Stop everything
```bash
docker-compose down       # Stop containers (keeps data)
docker-compose down -v    # Stop and wipe all data (fresh start)
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `SIGNAL_INTERVAL_MS` | `3000` | Simulator signal interval in ms (set in docker-compose.yml) |
| `ASPNETCORE_ENVIRONMENT` | `Docker` | Switches config to use container hostnames |

## Project Structure

```
SMS_EventScheduler/
├── docker-compose.yml           # Orchestrates all 4 services
├── EventScheduler.slnx          # .NET solution file
│
├── EventScheduler/              # .NET Backend API
│   ├── Dockerfile               # Multi-stage build (SDK → Runtime)
│   ├── Program.cs               # App startup, DI, auto-seeding
│   ├── appsettings.json         # Local dev config (localhost)
│   ├── appsettings.Docker.json  # Docker config (container hostnames)
│   ├── Controllers/
│   │   ├── SignalController.cs         # POST /api/signals
│   │   ├── EventsController.cs         # GET /api/events
│   │   ├── ConfigController.cs         # CRUD /api/config
│   │   └── ClassificationController.cs # CRUD /api/classification
│   ├── Services/
│   │   ├── SignalProcessingEngine.cs   # Singleton timer state machine
│   │   ├── OpcUaClientService.cs       # OPC-UA subscription client
│   │   ├── TimingEventService.cs       # PostgreSQL data access (raw SQL)
│   │   ├── InfluxDbService.cs          # InfluxDB time-series writer
│   │   └── Interfaces/
│   │       ├── ISignalProcessingEngine.cs
│   │       ├── ITimingEventService.cs
│   │       └── IInfluxDbService.cs
│   ├── Models/
│   │   ├── SignalConfig.cs
│   │   ├── TimingEvent.cs
│   │   ├── RawSignalLog.cs
│   │   └── Classification.cs
│   ├── DTOs/                    # Request/Response models
│   ├── Data/
│   │   └── AppDbContext.cs      # EF Core context (4 DbSets)
│   └── Migrations/              # EF Core migration history
│
└── OpcUaSimulator/              # Node.js Factory Simulator
    ├── Dockerfile               # Node 22 Alpine
    ├── simulator.js             # 3 sensors, configurable interval
    ├── package.json
    └── .gitignore
```

## Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Backend | ASP.NET Core | 10.0 |
| ORM | Entity Framework Core | 10.0.6 |
| OPC-UA Client | OPCFoundation SDK | 1.5.378 |
| Time-Series DB | InfluxDB.Client | 5.0.0 |
| Relational DB | PostgreSQL (Npgsql) | 16 |
| Simulator | Node.js + node-opcua | 22 + 2.172 |
| Containers | Docker Compose | Latest |

## Database Schema

### PostgreSQL Tables

- **RawSignalLogs** — Every signal received (audit trail)
- **SignalConfigs** — Maps signals to trigger types + classifications
- **TimingEvents** — Recorded delay events with duration
- **Classifications** — Hierarchical delay taxonomy (3 levels max)

### InfluxDB Measurements (Bucket: `factory-signals`)

- **raw_signals** — Tags: signalId, qualityFlag | Fields: value, qualityCode
- **timing_events** — Tags: triggerSignalId, endSignalId, classificationId | Fields: durationMs, durationFormatted

## Built By

**Anurag Drumagar** — Backend Developer Intern @ SMS Group
