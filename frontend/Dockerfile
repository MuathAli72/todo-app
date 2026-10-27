# The recipe for packing the frontend into a container that Kubernetes can run.
# Build it yourself from the frontend folder:  docker build -t todo-web .

# ---------- Stage 1: build ----------
# Start from an image that has Node, the tool that builds the website.
FROM node:24-alpine AS build
WORKDIR /src

# Copy only the package list first and install packages.
# Docker remembers this step, so packages are only downloaded again when the list changes.
COPY package.json package-lock.json ./
RUN npm ci

# Now copy the code and build the plain website files into /src/dist.
COPY . .
# All API calls go to /api. The front door sends /api to the API and strips the "/api" part.
ENV VITE_API_URL=/api
RUN npm run build

# ---------- Stage 2: run ----------
# Start again from a small web server (nginx) that only hands out files.
# The "unprivileged" version doesn't run as the all-powerful root user. Safer if it's ever attacked.
FROM nginxinc/nginx-unprivileged:1.31-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist /usr/share/nginx/html

# Ports below 1024 need root, so nginx listens on 8080 instead of 80.
EXPOSE 8080
