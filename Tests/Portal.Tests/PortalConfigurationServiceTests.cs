using Moq;
using Portal.DBLayer;
using Portal.DBServices;
using Erp.Domain.Models;
using Xunit;
using System.Text.Json;

namespace Portal.Tests.DBServices
{
    public class PortalConfigurationServiceTests
    {
        private readonly Mock<IPortalConfigurationDbLayer> _mockConfigDbLayer;
        private readonly PortalConfigurationService _service;

        public PortalConfigurationServiceTests()
        {
            _mockConfigDbLayer = new Mock<IPortalConfigurationDbLayer>();
            _service = new PortalConfigurationService(_mockConfigDbLayer.Object);
        }

        [Fact]
        public async Task GetPortalConfigurationByNameAsync_WithValidName_ReturnsConfigurations()
        {
            // Arrange
            var configurations = new List<PortalConfiguration>
            {
                new PortalConfiguration 
                { 
                    Id = 1, 
                    Name = "Portal", 
                    SysCode = "PORTAL",
                    Settings = "{\"key\": \"value\"}"
                }
            };

            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("Portal", "PORTAL"))
                .ReturnsAsync(configurations);

            // Act
            var result = await _service.GetPortalConfigurationByNameAsync("Portal");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Portal", result[0].Name);
            _mockConfigDbLayer.Verify(
                x => x.GetPortalConfigurationByNameAndCodeAsync("Portal", "PORTAL"),
                Times.Once);
        }

        [Fact]
        public async Task GetPortalConfigurationByNameAsync_WithInvalidName_ReturnsEmpty()
        {
            // Arrange
            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("InvalidName", "PORTAL"))
                .ReturnsAsync(new List<PortalConfiguration>());

            // Act
            var result = await _service.GetPortalConfigurationByNameAsync("InvalidName");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPortalConfigurationByNameAsync_CallsDbLayerWithCorrectParameters()
        {
            // Arrange
            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("TestConfig", "PORTAL"))
                .ReturnsAsync(new List<PortalConfiguration>());

            // Act
            await _service.GetPortalConfigurationByNameAsync("TestConfig");

            // Assert
            _mockConfigDbLayer.Verify(
                x => x.GetPortalConfigurationByNameAndCodeAsync("TestConfig", "PORTAL"),
                Times.Once);
        }

        [Fact]
        public async Task GetPortalAdminEmail_WithValidConfiguration_ReturnsEmail()
        {
            // Arrange
            var emailValue = "admin@example.com";
            var settings = new Dictionary<string, string> { { "AdministratorEmail", emailValue } };
            var jsonSettings = JsonSerializer.Serialize(settings);

            var configurations = new List<PortalConfiguration>
            {
                new PortalConfiguration
                {
                    Id = 1,
                    Name = "Portal",
                    SysCode = "PORTAL",
                    Settings = jsonSettings
                }
            };

            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("Portal", "PORTAL"))
                .ReturnsAsync(configurations);

            // Act
            var result = await _service.GetPortalAdminEmail();

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(emailValue, result);
        }

        [Fact]
        public async Task GetPortalAdminEmail_WithNoAdminEmailKey_ReturnsEmpty()
        {
            // Arrange
            var settings = new Dictionary<string, string> { { "OtherKey", "value" } };
            var jsonSettings = JsonSerializer.Serialize(settings);

            var configurations = new List<PortalConfiguration>
            {
                new PortalConfiguration
                {
                    Id = 1,
                    Name = "Portal",
                    SysCode = "PORTAL",
                    Settings = jsonSettings
                }
            };

            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("Portal", "PORTAL"))
                .ReturnsAsync(configurations);

            // Act
            var result = await _service.GetPortalAdminEmail();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPortalAdminEmail_WithEmptyConfigurationList_ReturnsEmpty()
        {
            // Arrange
            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("Portal", "PORTAL"))
                .ReturnsAsync(new List<PortalConfiguration>());

            // Act
            var result = await _service.GetPortalAdminEmail();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPortalAdminEmail_WithNullConfigurations_ReturnsEmpty()
        {
            // Arrange
            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("Portal", "PORTAL"))
                .ReturnsAsync((List<PortalConfiguration>)null);

            // Act
            var result = await _service.GetPortalAdminEmail();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPortalAdminEmail_WithMultipleConfigurations_ReturnsFirstAdminEmail()
        {
            // Arrange
            var emailValue = "admin@example.com";
            var settings = new Dictionary<string, string> { { "AdministratorEmail", emailValue } };
            var jsonSettings = JsonSerializer.Serialize(settings);

            var configurations = new List<PortalConfiguration>
            {
                new PortalConfiguration
                {
                    Id = 1,
                    Name = "Portal",
                    SysCode = "PORTAL",
                    Settings = jsonSettings
                },
                new PortalConfiguration
                {
                    Id = 2,
                    Name = "Portal2",
                    SysCode = "PORTAL",
                    Settings = jsonSettings
                }
            };

            _mockConfigDbLayer
                .Setup(x => x.GetPortalConfigurationByNameAndCodeAsync("Portal", "PORTAL"))
                .ReturnsAsync(configurations);

            // Act
            var result = await _service.GetPortalAdminEmail();

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(emailValue, result);
        }
    }
}
