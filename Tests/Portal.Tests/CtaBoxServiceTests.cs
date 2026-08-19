using Moq;
using Portal.DBLayer;
using Portal.DBServices;
using Erp.Domain.Models;
using App2025.Portal.Models;
using Xunit;

namespace Portal.Tests.DBServices
{
    public class CtaBoxServiceTests
    {
        private readonly Mock<IPortalConfigurationDbLayer> _mockConfigDbLayer;
        private readonly CtaBoxService _service;

        public CtaBoxServiceTests()
        {
            _mockConfigDbLayer = new Mock<IPortalConfigurationDbLayer>();
            _service = new CtaBoxService(_mockConfigDbLayer.Object);
        }

        [Fact]
        public async Task GetCtaBoxInfoAsync_WithValidConfig_ReturnsCtaBoxViewModel()
        {
            // Arrange
            var jsonData = @"{""title"":""Test CTA"",""description"":""Test Description""}";
            var config = new PortalConfiguration
            {
                Id = 1,
                Name = "CtaBox",
                SysCode = "PORTAL",
                Settings = jsonData
            };

            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("CtaBox", "PORTAL"))
                .ReturnsAsync(new List<PortalConfiguration> { config });

            // Act
            var result = await _service.GetCtaBoxInfoAsync();

            // Assert
            Assert.NotNull(result);
            _mockConfigDbLayer.Verify(
                x => x.GetPortalConfigurationByNameAndCodeAsync("CtaBox", "PORTAL"),
                Times.Once);
        }

        [Fact]
        public async Task GetCtaBoxInfoAsync_WithEmptyList_ReturnsNull()
        {
            // Arrange
            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("CtaBox", "PORTAL"))
                .ReturnsAsync(new List<PortalConfiguration>());

            // Act
            var result = await _service.GetCtaBoxInfoAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCtaBoxInfoAsync_WithNullConfigs_ReturnsNull()
        {
            // Arrange
            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("CtaBox", "PORTAL"))
                .ReturnsAsync((List<PortalConfiguration>)null);

            // Act
            var result = await _service.GetCtaBoxInfoAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCtaBoxInfoAsync_WithNullSettings_ReturnsNull()
        {
            // Arrange
            var config = new PortalConfiguration
            {
                Id = 1,
                Name = "CtaBox",
                SysCode = "PORTAL",
                Settings = null
            };

            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("CtaBox", "PORTAL"))
                .ReturnsAsync(new List<PortalConfiguration> { config });

            // Act
            var result = await _service.GetCtaBoxInfoAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCtaBoxInfoAsync_WithEmptySettings_ReturnsNull()
        {
            // Arrange
            var config = new PortalConfiguration
            {
                Id = 1,
                Name = "CtaBox",
                SysCode = "PORTAL",
                Settings = ""
            };

            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("CtaBox", "PORTAL"))
                .ReturnsAsync(new List<PortalConfiguration> { config });

            // Act
            var result = await _service.GetCtaBoxInfoAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCtaBoxInfoAsync_WhenExceptionThrown_ReturnsNull()
        {
            // Arrange
            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("CtaBox", "PORTAL"))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.GetCtaBoxInfoAsync();

            // Assert
            Assert.Null(result);
        }
    }
}
