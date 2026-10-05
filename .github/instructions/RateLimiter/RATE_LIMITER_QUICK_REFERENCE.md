# Rate Limiter Quick Reference

## What Was Implemented

Rate limiting on `ArticlesController.Articles()` method:
- **Limit**: 10 requests per IP address per 60 seconds
- **Response**: HTTP 429 (Too Many Requests) when exceeded
- **Cache Backend**: Redis (with in-memory fallback)

## Files Created

| File | Purpose |
|------|---------|
| `Controllers/Attributes/RateLimitAttribute.cs` | Main rate limiting filter attribute |
| `Services/RateLimitSettings.cs` | Configuration class with settings |
| `Services/RateLimitSettings.example.json` | Example appsettings configuration |
| `RATE_LIMITER_IMPLEMENTATION.md` | Comprehensive documentation |

## Files Modified

| File | Change |
|------|--------|
| `Controllers/ArticlesController.cs` | Added using; Added [RateLimit] to Articles() |
| `Program.cs` | Registered RateLimitSettings |

## How to Use

### 1. Apply to a Method
```csharp
[HttpGet]
[Route("Articles/{viewName}")]
[RateLimit(requestsPerWindow: 10)]
public async Task<IActionResult> Articles(string viewName) { ... }
```

### 2. Configure in appsettings.json
```json
{
  "RateLimit": {
	"Enabled": true,
	"DefaultRequestsPerWindow": 10,
	"DefaultWindowSeconds": 60
  }
}
```

### 3. Test It
- Make requests to the Articles endpoint
- After 10 requests in 60 seconds:
  - Next request returns 429
  - Wait 60 seconds or until window expires
  - Counter resets and you can make 10 more requests

## Key Features

✅ Per-IP rate limiting  
✅ Configurable limits per endpoint  
✅ X-Forwarded-For header support (proxy scenarios)  
✅ Works with Redis + in-memory cache  
✅ Thread-safe atomic operations  
✅ Graceful degradation if cache unavailable  
✅ Returns HTTP 429 status code  

## Response When Limited

**Status Code**: 429 Too Many Requests

**Example with curl:**
```bash
# First 10 requests succeed
curl https://myapp.com/Articles/sample-article

# 11th request:
curl https://myapp.com/Articles/sample-article
# Response: 429 Too Many Requests
```

## Customization

### Per-Method Limits
```csharp
[RateLimit(requestsPerWindow: 5, windowSeconds: 30)]   // 5 per 30 sec
public async Task<IActionResult> ExpensiveAction() { ... }

[RateLimit(requestsPerWindow: 100, windowSeconds: 60)] // 100 per min
public async Task<IActionResult> CheapAction() { ... }
```

### Global Settings
In `appsettings.json`:
```json
"RateLimit": {
  "DefaultRequestsPerWindow": 10,
  "DefaultWindowSeconds": 60,
  "EndpointOverrides": {
	"Articles:Articles": { "RequestsPerWindow": 20, "WindowSeconds": 120 }
  }
}
```

## Monitoring

### Cache Key Format
Look for keys like: `rate-limit:192.168.1.100:Articles:Articles`

### Redis Commands (if using Redis)
```bash
# See all rate limit counters
KEYS rate-limit:*

# Check specific counter
GET rate-limit:192.168.1.100:Articles:Articles

# Check TTL (time to reset)
TTL rate-limit:192.168.1.100:Articles:Articles
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| 429 on every request | Check cache connectivity; verify configured limits |
| Rate limiting not working | Verify [RateLimit] attribute is applied |
| Different limits per server | Use Redis instead of in-memory cache |
| X-Forwarded-For not working | Ensure proxy is configured correctly |

## Next Steps

1. ✅ Attribute applied to Articles() method
2. ✅ Dependencies registered in Program.cs
3. ⚠️ (Optional) Add unit tests - See `Tests/Controllers/Attributes/RateLimitAttributeTests.md`
4. ⚠️ (Optional) Apply to other methods as needed
5. ⚠️ (Optional) Customize limits in appsettings.json

---

For complete documentation, see: **RATE_LIMITER_IMPLEMENTATION.md**
