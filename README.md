# OfficeBooking

[![CI](https://github.com/LukasMicek/OfficeBooking/actions/workflows/ci.yml/badge.svg)](https://github.com/LukasMicek/OfficeBooking/actions/workflows/ci.yml)

## Project Description
OfficeBooking is a conference room reservation system built with .NET 8 and ASP.NET Core MVC. The application allows employees to search for available rooms by date, capacity, and equipment, then create and manage their reservations. Administrators have full control over rooms, equipment, and all reservations.

## Key Features
The system implements several core capabilities:
- Room search with filtering by date range, minimum capacity, and required equipment
- Reservation management with automatic conflict detection and business rule validation
- Business hours enforcement (8:00-20:00) with maximum 8-hour booking duration
- User authentication via ASP.NET Core Identity with role-based access (Admin/User)
- Personal reservation dashboard with edit and cancel functionality
- Admin panel for room and equipment CRUD operations
- Equipment assignment to rooms via checkbox interface
- Admin ability to cancel any reservation with a reason
- Monthly room utilization reports (minutes/hours per room)

## Getting Started
To run locally:
1. Ensure .NET 8 SDK is installed
2. Clone the repository
3. Run `dotnet run --project OfficeBooking`

The application uses SQLite and automatically creates the database file on first run.

Alternatively, run with Docker Compose:
```bash
docker compose up
```

Or build and run manually:
```bash
docker build -t officebooking .
docker run -p 8080:8080 officebooking
```

## Development Seeding
In Development environment, you can seed an admin account and demo data using environment variables:

```bash
# Seed admin account
SEED_ADMIN=true \
SEED_ADMIN_EMAIL=your-email@example.com \
SEED_ADMIN_PASSWORD=YourSecurePassword123! \
dotnet run --project OfficeBooking

# Seed demo rooms and equipment
SEED_DEMO_DATA=true \
dotnet run --project OfficeBooking
```

| Variable | Description |
|----------|-------------|
| `SEED_ADMIN` | Set to `true` to enable admin seeding (Development only) |
| `SEED_ADMIN_EMAIL` | Admin account email address |
| `SEED_ADMIN_PASSWORD` | Admin account password (must meet Identity requirements) |
| `SEED_DEMO_DATA` | Set to `true` to seed sample rooms and equipment (Development only) |

**Note:** If `SEED_ADMIN_EMAIL` or `SEED_ADMIN_PASSWORD` are missing, admin seeding is skipped silently.

## Testing & CI/CD
The project uses xUnit with FluentAssertions for testing. Tests are organized into:
- **Unit tests**: Business logic validation (BookingRules, ReservationConflict)
- **Integration tests**: Service layer with in-memory SQLite database

Run tests with:
```bash
dotnet test
```

GitHub Actions runs automated builds and tests on every push to master.

## Technical Architecture
The codebase follows a layered architecture:
- **Controllers**: Thin controllers delegating to services
- **Services**: Business logic with TimeProvider abstraction for testability
- **Business**: Pure validation functions (BookingRules, ReservationConflict)
- **Data**: Entity Framework Core with SQLite, performance indexes for common queries
- **Models**: Domain entities and request/response DTOs

Key design decisions:
- TimeProvider abstraction enables deterministic time-based testing
- Reservation conflict detection uses half-open interval logic (adjacent bookings allowed)
- Service methods return result objects with Success/Error for explicit error handling
