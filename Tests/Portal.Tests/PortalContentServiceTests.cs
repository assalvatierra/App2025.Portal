using Moq;
using Portal.DBLayer;
using Portal.DBServices;
using Erp.Domain.Models;
using Portal.Models;
using Xunit;
using Microsoft.Extensions.Configuration;

namespace Portal.Tests.DBServices
{
    public class PortalContentServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IPortalContentDbLayer> _mockContentDbLayer;
        private readonly PortalContentService _service;

        public PortalContentServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockContentDbLayer = new Mock<IPortalContentDbLayer>();
            _service = new PortalContentService(_mockConfiguration.Object, _mockContentDbLayer.Object);
        }

        [Fact]
        public async Task GetAllActiveContentsAsync_WithValidContents_ReturnsMappedContents()
        {
            // Arrange
            var jsonData = @"{""title"":""Test Title"",""description"":""Test Description"",""imageUrl"":""http://example.com/image.jpg"",""pageUrl"":""http://example.com/page""}";
            var contents = new List<PortalContent>
            {
                new PortalContent 
                { 
                    Id = 1, 
                    Name = "Content 1",
                    JsonData = jsonData,
                    Status = "Active"
                }
            };

            _mockContentDbLayer
                .Setup(x => x.GetContentsByStatusAsync("Active"))
                .ReturnsAsync(contents);

            // Act
            var result = await _service.GetAllActiveContentsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Content 1", result[0].Content.Name);
            _mockContentDbLayer.Verify(
                x => x.GetContentsByStatusAsync("Active"),
                Times.Once);
        }

        [Fact]
        public async Task GetAllActiveContentsAsync_WithEmptyJsonData_ReturnsMappedWithDefaults()
        {
            // Arrange
            var contents = new List<PortalContent>
            {
                new PortalContent 
                { 
                    Id = 1, 
                    Name = "Content 1",
                    JsonData = "{}",
                    Status = "Active"
                }
            };

            _mockContentDbLayer
                .Setup(x => x.GetContentsByStatusAsync("Active"))
                .ReturnsAsync(contents);

            // Act
            var result = await _service.GetAllActiveContentsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetAllActiveContentsAsync_WithNullJsonData_ReturnsMappedWithDefaults()
        {
            // Arrange
            var contents = new List<PortalContent>
            {
                new PortalContent 
                { 
                    Id = 1, 
                    Name = "Content 1",
                    JsonData = null,
                    Status = "Active"
                }
            };

            _mockContentDbLayer
                .Setup(x => x.GetContentsByStatusAsync("Active"))
                .ReturnsAsync(contents);

            // Act
            var result = await _service.GetAllActiveContentsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetAllActiveContentsAsync_WithNoContents_ReturnsEmptyList()
        {
            // Arrange
            _mockContentDbLayer
                .Setup(x => x.GetContentsByStatusAsync("Active"))
                .ReturnsAsync(new List<PortalContent>());

            // Act
            var result = await _service.GetAllActiveContentsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetContentsByCategoryAsync_WithValidCategoryAndNoTemporaryContents_ReturnsMappedContents()
        {
            // Arrange
            var categories = new List<string> { "Electronics" };
            var jsonData = @"{""title"":""Test""}";
            var contents = new List<PortalContent>
            {
                new PortalContent 
                { 
                    Id = 1, 
                    Name = "Content 1",
                    JsonData = jsonData
                }
            };

            _mockConfiguration
                .SetupGet(x => x["TemporaryContents:Enabled"])
                .Returns("false");

            _mockContentDbLayer
                .Setup(x => x.GetContentsByCategoryAsync(categories, null))
                .ReturnsAsync(contents);

            // Act
            var result = await _service.GetContentsByCategoryAsync(categories, null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetContentsByCategoryAsync_WithTemporaryContentsEnabled_AddsTemporaryService()
        {
            // Arrange
            var categories = new List<string> { "Electronics" };
            var contents = new List<PortalContent>();

            _mockConfiguration
                .SetupGet(x => x["TemporaryContents:Enabled"])
                .Returns("true");

            _mockConfiguration
                .SetupGet(x => x["TemporaryContents:Promo"])
                .Returns("TemporaryPromo");

            _mockContentDbLayer
                .Setup(x => x.GetContentsByCategoryAsync(
                    It.Is<List<string>>(l => l.Contains("Electronics") && l.Contains("TemporaryPromo")), 
                    "Promo"))
                .ReturnsAsync(contents);

            // Act
            var result = await _service.GetContentsByCategoryAsync(categories, "Promo");

            // Assert
            Assert.NotNull(result);
            _mockContentDbLayer.Verify(
                x => x.GetContentsByCategoryAsync(
                    It.Is<List<string>>(l => l.Contains("TemporaryPromo")),
                    "Promo"),
                Times.Once);
        }

        [Fact]
        public async Task GetContentsByCategoryAsync_WithEmptyResult_ReturnsEmptyList()
        {
            // Arrange
            var categories = new List<string> { "Electronics" };

            _mockConfiguration
                .SetupGet(x => x["TemporaryContents:Enabled"])
                .Returns("false");

            _mockContentDbLayer
                .Setup(x => x.GetContentsByCategoryAsync(categories, null))
                .ReturnsAsync(new List<PortalContent>());

            // Act
            var result = await _service.GetContentsByCategoryAsync(categories, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsContent()
        {
            // Arrange
            var content = new PortalContent 
            { 
                Id = 1, 
                Name = "Content 1",
                JsonData = @"{""title"":""Test""}"
            };

            _mockContentDbLayer
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(content);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Content 1", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockContentDbLayer
                .Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((PortalContent)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }
    }
}
