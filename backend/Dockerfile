# The recipe for packing the backend into containers.
# It makes two images from the same code:
#   - the API (the default):        docker build -t todo-api .
#   - the migrations helper:        docker build --target migrate -t todo-migrate .

# ---------- Stage 1: build ----------
# Start from Microsoft's image that has the full .NET 10 SDK (the tools to build code).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first and download packages.
# Docker remembers this step, so packages are only downloaded again when a .csproj changes.
COPY TodoApp.Core/TodoApp.Core.csproj TodoApp.Core/
COPY TodoApp.Api/TodoApp.Api.csproj TodoApp.Api/
RUN dotnet restore TodoApp.Api/TodoApp.Api.csproj

# Now copy the code and build a release version into /app.
# The tests aren't copied: the CI pipeline already ran them before this.
COPY TodoApp.Core/ TodoApp.Core/
COPY TodoApp.Api/ TodoApp.Api/
RUN dotnet publish TodoApp.Api/TodoApp.Api.csproj --configuration Release --output /app --no-restore

# ---------- Stage 2: build the migrations helper ----------
# Packs all migrations into one small program ("efbundle") that applies them and exits.
# Only built when asked for with --target migrate, so the API build doesn't pay for it.
FROM build AS bundle
RUN dotnet tool install --global dotnet-ef --version '10.*'
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations bundle --project TodoApp.Api/TodoApp.Api.csproj --configuration Release --output /bundle/efbundle

# ---------- Stage 3: the migrations helper image ----------
# Small runtime image with just the bundle. Kubernetes runs it once before the API starts.
# Usage: ./efbundle --connection "<connection string>"
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrate
WORKDIR /app
COPY --from=bundle /bundle/efbundle .
USER $APP_UID
ENTRYPOINT ["./efbundle"]

# ---------- Stage 4: the API image (the default, because it's last) ----------
# Start again from a much smaller image that can only run .NET apps, not build them.
# Only the finished app from stage 1 is copied in, so the final container stays small.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /app .

# Render sends traffic to port 10000 by default, so the API listens there.
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000

# Run as a normal user instead of the all-powerful root user. Safer if the app is ever attacked.
USER $APP_UID

ENTRYPOINT ["dotnet", "TodoApp.Api.dll"]
