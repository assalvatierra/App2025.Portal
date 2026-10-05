// INSTRUCTIONS FOR SETTING UP RATE LIMIT TESTS
// ===============================================
// 
// To run the unit tests for RateLimitAttribute, create a dedicated test project:
// 1. Create a new xUnit test project: Portal.Tests (or similar)
// 2. Add NuGet dependencies:
//    - xunit (latest)
//    - xunit.runner.visualstudio (latest)
//    - Moq (latest)
// 3. Copy the test class from 'RateLimitAttributeTests.cs' into your test project
//
// The test project should reference the main Portal project to access these classes:
// - Portal.Controllers.Attributes.RateLimitAttribute
// - Portal.Services.ICache
//
// TEST COVERAGE
// =============
// The test suite (in the file below) covers these scenarios:
// 
// ✓ FirstRequest_ShouldSucceed_AndSetCacheCounter
//   - Verifies that the first request is allowed and counter is initialized
//
// ✓ NinthRequest_ShouldSucceed
//   - Verifies that the 9th request passes (under the 10-request limit)
//
// ✓ TenthRequest_ShouldSucceed
//   - Verifies that the 10th request passes (exactly at the limit)
//
// ✓ EleventhRequest_ShouldBeLocked_AndReturn429
//   - CRITICAL TEST: Verifies that the 11th request is blocked with HTTP 429
//   - Confirms rate limit enforcement works correctly
//
// ✓ RateLimitExceeded_ShouldNotCallNext
//   - Verifies pipeline short-circuits when limit exceeded (action not invoked)
//
// ✓ NoCacheService_ShouldProceedNormally
//   - Verifies graceful degradation if cache service is unavailable
//
// ✓ CorrectCacheKeyFormat
//   - Verifies cache key follows: "rate-limit:{ip}:{controller}:{action}"
//
// ✓ CustomWindow_ShouldUseProvidedWindowSeconds
//   - Verifies configurable time window works correctly
//
// KEY IMPLEMENTATION NOTES
// ========================
// - The attribute uses IAsyncActionFilter for proper async support
// - Cache key format: "rate-limit:{clientIp}:{controller}:{action}"
// - Cache TTL: Configurable, default 60 seconds
// - Supports X-Forwarded-For header for proxy scenarios
// - Returns HTTP 429 (StatusCodes.Status429TooManyRequests) when limit exceeded
// - Gracefully degrades if cache service is unavailable
//
// RUNNING THE TESTS
// =================
// dotnet test Portal.Tests.csproj
// 
// Or in Visual Studio:
// Test Explorer > Run All Tests (Ctrl+R, A)
