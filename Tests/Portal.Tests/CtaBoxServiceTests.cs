using Moq;
using Portal.DBLayer;
using Portal.DBServices;
using Erp.Domain.Models;
using App2025.Portal.Models;
using Microsoft.Extensions.Logging;
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
            var mockLogger = new Mock<ILogger<CtaBoxService>>();
            _service = new CtaBoxService(_mockConfigDbLayer.Object, mockLogger.Object);
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

    }
}
