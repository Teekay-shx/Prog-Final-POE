# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# RaceDay.Web: ASP.NET Core MVC front end.
# Build context is this folder (src/RaceDay.Web); the web app has no project
# references because it only talks to the RaceDay API over HTTP.
#
#   docker build -t raceday-web ./src/RaceDay.Web
# ---------------------------------------------------------------------------

# ---- Stage 1: restore + publish with the full SDK ------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file first so the (slow) restore layer is cached until
# the package list changes, not on every source edit.
COPY RaceDay.Web.csproj ./
RUN dotnet restore RaceDay.Web.csproj

COPY . ./
RUN dotnet publish RaceDay.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---- Stage 2: small runtime image, no SDK ---------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Listen on 8080 (the non-root default for .NET 8 images) and run in Production,
# so users get friendly error pages and never a stack trace.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

# Where the RaceDay API lives. Override at run time with -e Api__BaseUrl=...
ENV Api__BaseUrl=http://host.docker.internal:5249

COPY --from=build /app/publish ./

# Never run as root: the .NET 8 images ship an unprivileged "app" user.
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
