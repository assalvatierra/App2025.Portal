# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["Portal.csproj", "./"]
RUN dotnet restore "Portal.csproj"

# Copy remaining source code
COPY . .

# Build the application
RUN dotnet build "Portal.csproj" -c Release -o /app/build

# Publish stages

FROM build AS publish
RUN dotnet publish "Portal.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port (default ASP.NET Core port)
EXPOSE 8080

# Set environment variable for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
	CMD curl -f http://localhost:8080 || exit 1

# Start the application
ENTRYPOINT ["dotnet", "Portal.dll"]
