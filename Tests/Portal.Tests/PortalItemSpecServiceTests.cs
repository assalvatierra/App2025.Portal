using Moq;
using Portal.DBLayer;
using Portal.DBServices;
using Erp.Domain.Models;
using Xunit;

namespace Portal.Tests.DBServices
{
    public class PortalItemSpecServiceTests
    {
        private readonly Mock<IPortalItemSpecDbLayer> _mockDb;
        private readonly PortalItemSpecService _service;

        public PortalItemSpecServiceTests()
        {
            _mockDb = new Mock<IPortalItemSpecDbLayer>();
            _service = new PortalItemSpecService(_mockDb.Object);
        }

        [Fact]
        public async Task GetAllAsync_WithValidSpecs_ReturnsAllSpecs()
        {
            // Arrange
            var specs = new List<PortalItemSpec>
            {
                new PortalItemSpec { Id = 1, PortalItemId = 1, JsonData = "{\"color\":\"Red\"}" },
                new PortalItemSpec { Id = 2, PortalItemId = 1, JsonData = "{\"size\":\"Large\"}" }
            };

            _mockDb
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(specs);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockDb.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_WithNoSpecs_ReturnsEmptyList()
        {
            // Arrange
            _mockDb
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<PortalItemSpec>());

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByPortalItemIdAsync_WithValidId_ReturnsSpecs()
        {
            // Arrange
            var specs = new List<PortalItemSpec>
            {
                new PortalItemSpec { Id = 1, PortalItemId = 1, JsonData = "{\"color\":\"Red\"}" },
                new PortalItemSpec { Id = 2, PortalItemId = 1, JsonData = "{\"size\":\"Large\"}" }
            };

            _mockDb
                .Setup(x => x.GetByPortalItemIdAsync(1))
                .ReturnsAsync(specs);

            // Act
            var result = await _service.GetByPortalItemIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, spec => Assert.Equal(1, spec.PortalItemId));
        }

        [Fact]
        public async Task GetByPortalItemIdAsync_WithInvalidId_ReturnsEmptyList()
        {
            // Arrange
            _mockDb
                .Setup(x => x.GetByPortalItemIdAsync(999))
                .ReturnsAsync(new List<PortalItemSpec>());

            // Act
            var result = await _service.GetByPortalItemIdAsync(999);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsSpec()
        {
            // Arrange
            var spec = new PortalItemSpec 
            { 
                Id = 1, 
                PortalItemId = 1, 
                JsonData = "{\"color\":\"Red\"}" 
            };

            _mockDb
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(spec);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("{\"color\":\"Red\"}", result.JsonData);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockDb
                .Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((PortalItemSpec)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_CallsDbLayerUpdate()
        {
            // Arrange
            var spec = new PortalItemSpec 
            { 
                Id = 1, 
                PortalItemId = 1, 
                JsonData = "{\"color\":\"Blue\"}" 
            };

            _mockDb
                .Setup(x => x.UpdateAsync(spec))
                .Returns(Task.CompletedTask);

            // Act
            await _service.UpdateAsync(spec);

            // Assert
            _mockDb.Verify(x => x.UpdateAsync(spec), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WithValidSpec_ReturnsAddedSpec()
        {
            // Arrange
            var spec = new PortalItemSpec 
            { 
                PortalItemId = 1, 
                JsonData = "{\"color\":\"Green\"}" 
            };

            var addedSpec = new PortalItemSpec 
            { 
                Id = 1,
                PortalItemId = 1, 
                JsonData = "{\"color\":\"Green\"}" 
            };

            _mockDb
                .Setup(x => x.AddAsync(spec))
                .ReturnsAsync(addedSpec);

            // Act
            var result = await _service.AddAsync(spec);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("{\"color\":\"Green\"}", result.JsonData);
            _mockDb.Verify(x => x.AddAsync(spec), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_CallsDbLayerDelete()
        {
            // Arrange
            var spec = new PortalItemSpec 
            { 
                Id = 1, 
                PortalItemId = 1, 
                JsonData = "{\"color\":\"Red\"}" 
            };

            _mockDb
                .Setup(x => x.DeleteAsync(spec))
                .Returns(Task.CompletedTask);

            // Act
            await _service.DeleteAsync(spec);

            // Assert
            _mockDb.Verify(x => x.DeleteAsync(spec), Times.Once);
        }

        [Fact]
        public void Exists_WithExistingId_ReturnsTrue()
        {
            // Arrange
            _mockDb
                .Setup(x => x.Exists(1))
                .Returns(true);

            // Act
            var result = _service.Exists(1);

            // Assert
            Assert.True(result);
            _mockDb.Verify(x => x.Exists(1), Times.Once);
        }

        [Fact]
        public void Exists_WithNonexistentId_ReturnsFalse()
        {
            // Arrange
            _mockDb
                .Setup(x => x.Exists(999))
                .Returns(false);

            // Act
            var result = _service.Exists(999);

            // Assert
            Assert.False(result);
            _mockDb.Verify(x => x.Exists(999), Times.Once);
        }
    }
}
