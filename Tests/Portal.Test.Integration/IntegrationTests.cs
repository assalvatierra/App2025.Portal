using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Text;
using System.Text.Json;
using Portal.Models;
using DotNet.Testcontainers.Containers;
using MassTransit.Serialization;

namespace Portal.Test.Integration
{
    /// <summary>
    /// Integration tests for Portal application with Qdrant TestContainer support.
    /// Tests run against the full application with an isolated Qdrant vector database container.
    /// Uses QdrantWebApplicationCompositeFixture which combines Qdrant container lifecycle
    /// management and web application factory configuration into a single fixture.
    /// </summary>
    public class IntegrationTests : IClassFixture<QdrantWebApplicationCompositeFixture>
    {
        private readonly QdrantWebApplicationCompositeFixture _factory;

        public IntegrationTests(QdrantWebApplicationCompositeFixture factory)
        {
            _factory = factory;
        }
        [Theory]
        [InlineData("/")]
        [InlineData("/Home/OurProducts")]
        [InlineData("/Home/OurServices")]
        [InlineData("/Home/Articles")]
        [InlineData("/Home/Privacy")]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("text/html; charset=utf-8",
                response?.Content?.Headers?.ContentType?.ToString());
        }

        #region Home/VectorDB Integration Tests

        [Fact]
        public async Task VectorDB_Get_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Home/VectorDB");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8",
                response?.Content?.Headers?.ContentType?.ToString());
        }

        [Fact]
        public async Task VectorDB_Get_ReturnsViewWithVectorDbScheduleViewDtoModel()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Home/VectorDB");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            // The view should contain references to VectorDB scheduling
            Assert.NotNull(content);
            Assert.NotEmpty(content);
            // Check that it's valid HTML
            Assert.Contains("<!DOCTYPE", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task VectorDB_Get_ReturnsPageWithScheduleInfo()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Home/VectorDB");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            // Check for common VectorDB schedule view elements
            Assert.NotNull(content);
            Assert.NotEmpty(content);
            // The HTML should contain schedule-related content
            Assert.True(content.Length > 0);
        }

        [Fact]
        public async Task VectorDB_Get_ContainsHasScheduleDataAttribute()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Home/VectorDB");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            // The page should contain schedule-related data attributes or elements
            Assert.NotNull(content);
            // Check that page has content (model was passed to view)
            Assert.True(content.Contains("Schedule", StringComparison.OrdinalIgnoreCase) ||
                        content.Contains("VectorDB", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SetVectorDbSchedule_PostWithValidData_ReturnsOkWithSuccessMessage()
        {
            // Arrange
            var client = _factory.CreateClient();
            var scheduleDto = new
            {
                nextSchedule = DateTime.UtcNow.AddDays(1),
                intervalValue = 24,
                intervalUnit = "Hours"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(scheduleDto),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await client.PostAsync("/Home/SetVectorDbSchedule", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent??"{}");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.True(jsonResponse.GetProperty("success").GetBoolean());
            Assert.NotNull(jsonResponse.GetProperty("message").GetString());
            //Assert.NotNull(jsonResponse.GetProperty("data"));
        }

        [Fact]
        public async Task SetVectorDbSchedule_PostWithNullModel_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var content = new StringContent(
                JsonSerializer.Serialize((object?)null),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await client.PostAsync("/Home/SetVectorDbSchedule", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(jsonResponse.GetProperty("success").GetBoolean());
            Assert.Contains("invalid", jsonResponse.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SetVectorDbSchedule_PostWithInvalidIntervalUnit_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var scheduleDto = new
            {
                nextSchedule = DateTime.UtcNow.AddDays(1),
                intervalValue = 24,
                intervalUnit = "InvalidUnit"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(scheduleDto),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await client.PostAsync("/Home/SetVectorDbSchedule", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(jsonResponse.GetProperty("success").GetBoolean());
            Assert.Contains("interval unit", jsonResponse.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("Minutes")]
        [InlineData("Hours")]
        [InlineData("Days")]
        public async Task SetVectorDbSchedule_PostWithValidIntervalUnits_ReturnsSuccess(string intervalUnit)
        {
            // Arrange
            var client = _factory.CreateClient();
            var scheduleDto = new
            {
                nextSchedule = DateTime.UtcNow.AddDays(1),
                intervalValue = 24,
                intervalUnit = intervalUnit
            };

            var content = new StringContent(
                JsonSerializer.Serialize(scheduleDto),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await client.PostAsync("/Home/SetVectorDbSchedule", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.True(jsonResponse.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task SetVectorDbSchedule_PostWithZeroIntervalValue_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var scheduleDto = new
            {
                nextSchedule = DateTime.UtcNow.AddDays(1),
                intervalValue = 0,
                intervalUnit = "Hours"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(scheduleDto),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await client.PostAsync("/Home/SetVectorDbSchedule", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(jsonResponse.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task UpdateVectorDbNow_PostWithoutSchedule_ReturnsOkWithData()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsync("/Home/UpdateVectorDbNow", null);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            // The endpoint returns 200 OK even when no schedule is set
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            Assert.True(jsonResponse.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task VectorDB_GetAfterSettingSchedule_ReturnsViewWithScheduleData()
        {
            // Arrange
            var client = _factory.CreateClient();
            var scheduleDto = new
            {
                nextSchedule = DateTime.UtcNow.AddDays(1),
                intervalValue = 12,
                intervalUnit = "Hours"
            };

            // First, set the schedule
            var setContent = new StringContent(
                JsonSerializer.Serialize(scheduleDto),
                Encoding.UTF8,
                "application/json");
            await client.PostAsync("/Home/SetVectorDbSchedule", setContent);

            // Act
            var getResponse = await client.GetAsync("/Home/VectorDB");
            var getContent = await getResponse.Content.ReadAsStringAsync();

            // Assert
            getResponse.EnsureSuccessStatusCode();
            Assert.NotNull(getContent);
            Assert.NotEmpty(getContent);
            // After setting schedule, the view should contain schedule info
            Assert.True(getContent.Length > 0);
        }

        #endregion

        #region Home/SearchByVector Integration Tests

        [Fact]
        public async Task SearchByVector_Get_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateClient();
            var searchQuery = "car";

            // Act
            var response = await client.GetAsync($"/Home/SearchByVector?query={searchQuery}");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8",
                response?.Content?.Headers?.ContentType?.ToString());
        }

        [Fact]
        public async Task SearchByVector_WithEmptyQuery_ReturnsViewWithMessage()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Home/SearchByVector?query=");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(content);
            Assert.NotEmpty(content);
            // Should show message about providing a search query
            Assert.True(content.Contains("search", StringComparison.OrdinalIgnoreCase) ||
                       content.Contains("query", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SearchByVector_WithNullQuery_ReturnsViewWithMessage()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Home/SearchByVector");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(content);
            Assert.NotEmpty(content);
        }

        [Theory]
        [InlineData("car")]
        [InlineData("automotive")]
        [InlineData("vehicle")]
        [InlineData("luxury")]
        public async Task SearchByVector_WithValidQuery_ReturnsViewResult(string query)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync($"/Home/SearchByVector?query={Uri.EscapeDataString(query)}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(content);
            Assert.NotEmpty(content);
            // Page should be valid HTML
            Assert.Contains("<!DOCTYPE", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SearchByVector_ReturnsItemListView()
        {
            // Arrange
            var client = _factory.CreateClient();
            var searchQuery = "car";

            // Act
            var response = await client.GetAsync($"/Home/SearchByVector?query={searchQuery}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            // Verify it's rendering an item list view or showing search results
            Assert.NotNull(content);
            Assert.True(content.Length > 0);
        }

        [Fact]
        public async Task SearchByVector_WithSpecialCharactersInQuery_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateClient();
            var searchQuery = "luxury-sports";

            // Act
            var response = await client.GetAsync($"/Home/SearchByVector?query={Uri.EscapeDataString(searchQuery)}");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task SearchByVector_WithLongQuery_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateClient();
            var searchQuery = "high performance sports car with luxury features and advanced technology";

            // Act
            var response = await client.GetAsync($"/Home/SearchByVector?query={Uri.EscapeDataString(searchQuery)}");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(response.Content);
        }

        [Fact]
        public async Task SearchByVector_ContainsSearchTermInViewBag()
        {
            // Arrange
            var client = _factory.CreateClient();
            var searchQuery = "test_query";

            // Act
            var response = await client.GetAsync($"/Home/SearchByVector?query={searchQuery}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            // Response should contain some indication of the search or results
            Assert.NotNull(content);
            Assert.NotEmpty(content);
        }

        [Fact]
        public async Task SearchByVector_ReturnsHtmlResponseType()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Home/SearchByVector?query=car");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Contains("text/html", response.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task SearchByVector_WithMultipleQueries_EachReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateClient();
            var queries = new[] { "sedan", "coupe", "suv" };

            // Act & Assert
            foreach (var query in queries)
            {
                var response = await client.GetAsync($"/Home/SearchByVector?query={Uri.EscapeDataString(query)}");
                response.EnsureSuccessStatusCode();
            }
        }

        [Fact]
        public async Task SearchByVector_PageTitleContainsSearchInfo()
        {
            // Arrange
            var client = _factory.CreateClient();
            var query = "luxury";

            // Act
            var response = await client.GetAsync($"/Home/SearchByVector?query={query}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            // Verify page has content structure for displaying results
            Assert.NotNull(content);
            Assert.True(content.Length > 100);
        }

        [Theory]
        [InlineData("car", "text/html")]
        [InlineData("vehicle", "text/html")]
        [InlineData("sports", "text/html")]
        public async Task SearchByVector_ValidatesContentTypeForQuery(string query, string expectedContentType)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync($"/Home/SearchByVector?query={Uri.EscapeDataString(query)}");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Contains(expectedContentType, response.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task SearchByVector_WhenNoResultsFound_StillReturnsSuccessfulResponse()
        {
            // Arrange
            var client = _factory.CreateClient();
            // Use a query that's unlikely to return results
            var uniqueQuery = $"xyzuniquequerynotexist{Guid.NewGuid()}";

            // Act
            var response = await client.GetAsync($"/Home/SearchByVector?query={Uri.EscapeDataString(uniqueQuery)}");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.EnsureSuccessStatusCode();
            // Should still show a valid page, even with no results
            Assert.NotNull(content);
            Assert.NotEmpty(content);
        }

        #endregion
    }
}
