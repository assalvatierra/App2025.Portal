# Docker Instructions

## Prerequisites
- Docker Desktop installed
- Docker Hub account (for uploading images)

## Build the Image

```
# Build with a tag
docker build -t app2025portal:latest .

# Build with version tag
docker build -t app2025portal:1.0.0 .

# Build without cache
docker build --no-cache -t app2025portal:latest .

# Build from specific Dockerfile
docker build -f ./Dockerfile -t app2025portal:latest .
```

## Tag the Image

```
# Tag for Docker Hub (format: username/repository:tag)
docker tag app2025portal:latest <your-docker-username>/app2025portal:latest

# Tag with version
docker tag app2025portal:1.0.0 <your-docker-username>/app2025portal:1.0.0

# Tag as latest as well
docker tag app2025portal:1.0.0 <your-docker-username>/app2025portal:latest
```

## Run the Image Locally

```
# Run in background with port mapping
docker run -d -p 80:80 -p 443:443 --name app2025 app2025portal:latest

# Run with environment variables
docker run -d -p 80:80 -p 443:443 -e ASPNETCORE_ENVIRONMENT=Production --name app2025 app2025portal:latest

# Run in interactive mode (for debugging)
docker run -it -p 80:80 -p 443:443 app2025portal:latest

# Run with volume mount
docker run -d -p 80:80 -p 443:443 -v C:\logs:/app/logs --name app2025 app2025portal:latest
```

## Manage Running Containers

```
# List running containers
docker ps

# List all containers
docker ps -a

# View container logs
docker logs <container-id>

# View live logs
docker logs -f <container-id>

# Stop container
docker stop <container-id>

# Remove container
docker rm <container-id>

# Restart container
docker restart <container-id>
```

## Upload to Docker Hub

```
# Login to Docker Hub
docker login

# Push image to Docker Hub
docker push <your-docker-username>/app2025portal:latest

# Push specific version
docker push <your-docker-username>/app2025portal:1.0.0

# Push all tags
docker push <your-docker-username>/app2025portal:latest
docker push <your-docker-username>/app2025portal:1.0.0
```

## Complete Workflow Example

```
# 1. Build the image
docker build -t app2025portal:1.0.0 .

# 2. Tag for Docker Hub
docker tag app2025portal:1.0.0 <your-docker-username>/app2025portal:1.0.0
docker tag app2025portal:1.0.0 <your-docker-username>/app2025portal:latest

# 3. Login to Docker Hub
docker login

# 4. Push to Docker Hub
docker push <your-docker-username>/app2025portal:1.0.0
docker push <your-docker-username>/app2025portal:latest

# 5. Run the image locally
docker run -d -p 80:80 -p 443:443 --name app2025 app2025portal:latest

# 6. Verify it's running
docker ps

# 7. Check logs
docker logs app2025
```

## Useful Commands

```
# Remove image
docker rmi app2025portal:latest

# Remove stopped containers
docker container prune

# Remove unused images
docker image prune

# View image details
docker inspect app2025portal:latest

# Build with build arguments
docker build --build-arg CONFIGURATION=Release -t app2025portal:latest .
```

## Environment Variables (ASP.NET Core)

```
# Common environment variables for running the container
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80;https://+:443
```

## Notes

- Replace `<your-docker-username>` with your actual Docker Hub username
- Replace `1.0.0` with your desired version number
- Ensure Docker Desktop is running before executing commands
- Use `docker logout` when done working with Docker Hub
