## Rate Limiting Telemetry & Logging Guide

### Overview
The rate limiting system now includes comprehensive logging at multiple levels to help you monitor, debug, and understand rate limit behavior.

### Log Levels Used

#### 1. **LogDebug** - Detailed Diagnostic Information
- **RateLimitAttribute**: Logs successful requests that passed the rate limit check
- **Format**: `"Rate limit OK - IP: {ClientIp}, Endpoint: {Endpoint}, Next count will be: {NextCount}"`
- **When**: Every successful request (very verbose, consider disabling in production)

#### 2. **LogInformation** - General Informational Messages
- **RateLimitAttribute**: Logs every rate limit check attempt
  - `"Rate limit check - IP: {ClientIp}, Endpoint: {Endpoint}, Count: {RequestCount}/{Limit}, Window: {WindowSeconds}s"`

- **ErrorController.RateLimitReached**: Logs when user is redirected to rate limit page
  - `"User hit rate limit - ReturnUrl: {ReturnUrl}, Limit: {RequestsPerWindow} requests per {WindowSeconds}s, RemoteIP: {RemoteIP}"`
  - `"RateLimitReached model created - Reset time: {ResetTime}"`

#### 3. **LogWarning** - Rate Limit Violations
- **RateLimitAttribute**: ONLY logged when rate limit is exceeded
- **Format**: `"Rate limit EXCEEDED - IP: {ClientIp}, Endpoint: {Endpoint}, Requests: {RequestCount}, Limit: {RequestsPerWindow}"`
- **Action**: Triggers redirect to rate limit page
- **Use Case**: Monitor for potential abuse or attack patterns

#### 4. **LogError** - Exception Handling
- **RateLimitAttribute**: Any unhandled exception in the rate limiter
- **Format**: `"Error in RateLimitAttribute - request may bypass rate limiting"`
- **Importance**: CRITICAL - indicates rate limiter malfunction

### Log Data Fields

| Field | Source | Description |
|-------|--------|-------------|
| `ClientIp` | `GetClientIpAddress()` | Client's IP address (checks X-Forwarded-For first) |
| `Endpoint` | Route data | Format: `ControllerName:ActionName` (e.g., `Articles:Articles`) |
| `RequestCount` | Cache | Current request count for this IP/endpoint in the window |
| `Limit` | Configuration | Configured request limit from appsettings.json |
| `WindowSeconds` | Configuration | Time window in seconds for the limit |
| `RemoteIP` | HttpContext | Client's remote IP address |
| `NextCount` | Calculated | What the count will be after this request |
| `ResetTime` | Calculated | When the rate limit counter resets |

### Configuration Location
All logging settings are controlled by Serilog configuration in `appsettings.json`:

```json
"Serilog": {
  "MinimumLevel": "Information",  // Change to "Debug" for verbose logging
  "WriteTo": [
	{ "Name": "Console", ... },
	{ "Name": "File", ... }
  ],
  "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
}
```

### Recommended Log Monitoring Strategy

#### **Development Environment**
- Set `MinimumLevel` to `"Debug"` to see all rate limit checks
- Useful for testing and understanding the rate limit behavior

#### **Production Environment**
- Keep `MinimumLevel` at `"Information"` or `"Warning"`
- Only log rate limit violations and errors
- Monitor the Warning logs for potential abuse patterns
- Set up alerts for repeated LogWarning entries from the same IP

### Example Log Output

```
[INF] Rate limit check - IP: 192.168.1.100, Endpoint: Articles:Articles, Count: 1/3, Window: 60s
[INF] Rate limit check - IP: 192.168.1.100, Endpoint: Articles:Articles, Count: 2/3, Window: 60s
[INF] Rate limit check - IP: 192.168.1.100, Endpoint: Articles:Articles, Count: 3/3, Window: 60s
[WRN] Rate limit EXCEEDED - IP: 192.168.1.100, Endpoint: Articles:Articles, Requests: 4, Limit: 3
[INF] User hit rate limit - ReturnUrl: /Articles/SomeView, Limit: 3 requests per 60s, RemoteIP: 192.168.1.100
[INF] RateLimitReached model created - Reset time: 12/19/2024 14:32:15
```

### Troubleshooting with Logs

#### Issue: Rate limiter not activating
- **Check**: Debug logs show "Rate limit check" messages
- **If missing**: Verify `[RateLimit]` attribute is applied and `RateLimit.Enabled` is `true` in config

#### Issue: "Error in RateLimitAttribute" messages
- **Indicates**: Cache service not available or configuration missing
- **Check**: Redis connection or InMemoryCache registration in Program.cs
- **Check**: RateLimitSettings bound in Program.cs

#### Issue: Rate limits not enforcing at configured values
- **Check**: Log shows actual `Count` vs `Limit` values
- **Compare**: With `appsettings.Development.json` endpoint overrides
- **Verify**: Endpoint key format matches (e.g., `Articles:Articles`)

### Performance Considerations

- **Debug logging** generates high volume - ~1-2 log entries per request per endpoint
- **Information logging** only logs violations and entry/exit
- **Recommended production setting**: Information level or higher
- Logs include minimal data overhead (~100-200 bytes per structured log entry)

### Integration with Existing Logging

The rate limiting logs integrate with your existing Serilog configuration:
- Uses the same logger as the rest of your application
- Respects the same MinimumLevel and filters
- Can be piped to File, Console, CloudWatch, etc.

### Testing the Logs

To see the logs in action:
1. Set `"MinimumLevel": "Debug"` in your appsettings.Development.json
2. Hit the Articles endpoint 3+ times quickly
3. Check the console/file output for the log messages
4. On the 4th request, you should see the "EXCEEDED" warning
