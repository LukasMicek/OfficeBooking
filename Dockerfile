# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY OfficeBooking.sln ./
COPY OfficeBooking/OfficeBooking.csproj OfficeBooking/
COPY OfficeBooking.Tests/OfficeBooking.Tests.csproj OfficeBooking.Tests/

RUN dotnet restore OfficeBooking.sln

COPY . .
RUN dotnet publish OfficeBooking/OfficeBooking.csproj -c Release -o /app/publish --no-restore

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/officebooking.db"

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser \
    && mkdir -p /app/data \
    && chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OfficeBooking.dll"]


