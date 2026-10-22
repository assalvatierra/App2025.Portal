# GitHub Secrets Configuration for Integration Tests

## Overview
This document outlines all GitHub secrets required to successfully run integration tests in GitHub Actions CI/CD pipeline.

## Required Secrets

### 1. **DB_CONNECTION_STRING** ⭐ REQUIRED
**Purpose:** Database connection for integration tests  
**Environment Variable:** `ConnectionStrings__DefaultConnection`  
**Status:** ✅ Already configured in CI workflow

**Example value for SQL Server:**
```
Server=your-server.database.windows.net;Initial Catalog=your-test-db;User Id=your-user;Password=your-password;
```

**Example value for local SQL Server:**
```
Server=(localdb)\mssqllocaldb;Database=aspnet-Portal-Testing;Trusted_Connection=True;MultipleActiveResultSets=true
```

**Steps to set up:**
1. Go to: **GitHub Repository → Settings → Secrets and variables → Actions**
2. Click **"New repository secret"**
3. Name: `DB_CONNECTION_STRING`
4. Value: Your SQL Server connection string
5. Click **"Add secret"**

---

### 2. **CODECOV_TOKEN** (Optional)
**Purpose:** Code coverage reporting to Codecov  
**Environment Variable:** `CODECOV_TOKEN`  
**Status:** ⚠️ Optional - CI will proceed if not set  

**Steps to set up:**
1. Register at https://codecov.io (free for public repos)
2. Connect your GitHub repository
3. Copy the repository token
4. Go to: **GitHub Repository → Settings → Secrets and variables → Actions**
5. Click **"New repository secret"**
6. Name: `CODECOV_TOKEN`
7. Value: Your Codecov token
8. Click **"Add secret"**

---

### 3. **GH_PAT** (Optional - for Docker image push)
**Purpose:** GitHub Personal Access Token for pushing Docker images to GitHub Container Registry  
**Environment Variable:** Used in Docker login step  
**Status:** ⚠️ Optional - only needed if pushing to GHCR

**Steps to set up:**
1. Go to: **GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)**
2. Click **"Generate new token (classic)"**
3. Set scopes: `write:packages`, `read:packages`, `delete:packages`
4. Generate and copy the token
5. Go to: **Repository → Settings → Secrets and variables → Actions**
6. Click **"New repository secret"**
7. Name: `GH_PAT`
8. Value: Your PAT token
9. Click **"Add secret"**

---

## Configuration Summary

| Secret Name | Required | Used For | CI Step |
|---|---|---|---|
| `DB_CONNECTION_STRING` | ✅ Yes | Database connection | Integration Tests |
| `CODECOV_TOKEN` | ⚠️ Optional | Code coverage upload | Upload coverage reports |
| `GH_PAT` | ⚠️ Optional | Docker image push | Docker build & push |

---

## Current CI Workflow Variables

The workflow uses these environment variables (automatically provided by GitHub):

```yaml
${{ github.actor }}           # Current user pushing code
${{ secrets.GITHUB_TOKEN }}   # Built-in GitHub token (always available)
${{ github.event_name }}      # Event type: push or pull_request
${{ github.repository_owner }} # Repository owner
${{ github.event.repository.name }} # Repository name
```

---

## Testing Locally

To test the same configuration locally:

```powershell
# Set environment variables (Windows PowerShell)
$env:ASPNETCORE_ENVIRONMENT = "Testing"
$env:ConnectionStrings__DefaultConnection = "your-connection-string"

# Run integration tests
dotnet test Tests/Portal.Test.Integration --configuration Release
```

---

## Troubleshooting

### Error: "Connection string 'DefaultConnection' not found"
- ✅ **Fix:** Ensure `DB_CONNECTION_STRING` secret is set in GitHub

### Error: "Token required - not valid tokenless upload"
- ✅ **Fix:** This is from Codecov. Either:
  - Set `CODECOV_TOKEN` secret, OR
  - This will be skipped automatically if not set (CI continues)

### Integration tests fail with connection timeout
- ✅ **Verify:** Database is accessible from GitHub Actions (Windows environment)
- ✅ **Verify:** Connection string is correct and includes `MultipleActiveResultSets=true`
- ✅ **Note:** Tests use `appsettings.Testing.json` which has Redis disabled

### Tests pass locally but fail in GitHub Actions
- ✅ **Verify:** Using same `DB_CONNECTION_STRING` value locally
- ✅ **Verify:** `ASPNETCORE_ENVIRONMENT` is set to `Testing`
- ✅ **Compare:** Check `appsettings.Testing.json` vs your local settings

---

## Configuration Files

### appsettings.Testing.json
Used in GitHub Actions to provide test-safe defaults:

```json
{
  "Caching": {
	"Redis": {
	  "enabled": false  // ✅ Redis disabled (uses in-memory cache)
	}
  },
  "MessageBroker": {
	"Idempotency": {
	  "EnableIdempotency": false  // ✅ Idempotency disabled
	}
  },
  "RateLimit": {
	"Enabled": false  // ✅ Rate limiting disabled
  }
}
```

This ensures tests don't require external services (Redis, RabbitMQ, etc.).

---

## Architecture

```
GitHub Actions Workflow
├── Set Environment Variables
│   ├── ASPNETCORE_ENVIRONMENT=Testing
│   └── ConnectionStrings__DefaultConnection=${{ secrets.DB_CONNECTION_STRING }}
├── Load Configuration (in priority order)
│   ├── 1. appsettings.json (base)
│   ├── 2. appsettings.Testing.json (override for Testing env)
│   └── 3. Environment variables (highest priority)
├── Build Solution
├── Run Tests (Unit → Integration → E2E)
├── Upload Coverage (if CODECOV_TOKEN set)
└── Build & Push Docker Image (if on main branch)
```

---

## Summary

To get integration tests running in GitHub Actions:

1. ✅ **Minimum Required:** Set `DB_CONNECTION_STRING` secret
2. ⚠️ **Recommended:** Set `CODECOV_TOKEN` for coverage tracking
3. ⚠️ **Optional:** Set `GH_PAT` for Docker image pushing

Once `DB_CONNECTION_STRING` is configured, integration tests should pass automatically! 🚀
