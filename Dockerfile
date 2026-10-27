# The image Render runs: the website AND the API in one container, on one address.
# One address means the login cookie works in every browser (see README, "Live hosting").
# Build it yourself from the repo root:  docker build -t todo-render .
# (Kubernetes still uses the separate images from backend/Dockerfile and frontend/Dockerfile.)

# ---------- Stage 1: build the website ----------
FROM node:24-alpine AS web
WORKDIR /web
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
# The website calls the API on the same address, under /api.
ENV VITE_API_URL=/api
RUN npm run build

# ---------- Stage 2: build the API ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/TodoApp.Core/TodoApp.Core.csproj TodoApp.Core/
COPY backend/TodoApp.Api/TodoApp.Api.csproj TodoApp.Api/
RUN dotnet restore TodoApp.Api/TodoApp.Api.csproj
COPY backend/TodoApp.Core/ TodoApp.Core/
COPY backend/TodoApp.Api/ TodoApp.Api/
RUN dotnet publish TodoApp.Api/TodoApp.Api.csproj --configuration Release --output /app --no-restore

# ---------- Stage 3: run both ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# ASP.NET serves whatever is in wwwroot as the website.
COPY --from=web /web/dist ./wwwroot

# Render sends traffic to port 10000 by default.
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000

# Run as a normal user instead of root. Safer if the app is ever attacked.
USER $APP_UID

ENTRYPOINT ["dotnet", "TodoApp.Api.dll"]
