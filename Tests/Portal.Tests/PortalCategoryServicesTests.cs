using Moq;
using Portal.DBLayer;
using Portal.DBServices;
using Erp.Domain.Models;
using Portal.Models;
using Xunit;

namespace Portal.Tests.DBServices
{
    public class PortalCategoryServicesTests
    {
        private readonly Mock<IPortalCategoryDbLayer> _mockDbLayer;
        private readonly PortalCategoryServices _service;

        public PortalCategoryServicesTests()
        {
            _mockDbLayer = new Mock<IPortalCategoryDbLayer>();
            _service = new PortalCategoryServices(_mockDbLayer.Object);
        }

        [Fact]
        public async Task GetAllByStatusAsync_WithValidStatus_ReturnsMappedCategories()
        {
            // Arrange
            var categories = new List<PortalCategory>
            {
                new PortalCategory { Id = 1, Name = "Category 1", Status = "Active" },
                new PortalCategory { Id = 2, Name = "Category 2", Status = "Active" }
            };

            _mockDbLayer
                .Setup(x => x.GetAllByStatusAsync("Active"))
                .ReturnsAsync(categories);

            // Act
            var result = await _service.GetAllByStatusAsync("Active");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockDbLayer.Verify(x => x.GetAllByStatusAsync("Active"), Times.Once);
        }

        [Fact]
        public async Task GetAllByStatusAsync_WithNullStatus_ReturnsMappedCategories()
        {
            // Arrange
            var categories = new List<PortalCategory>
            {
                new PortalCategory { Id = 1, Name = "Category 1", Status = "Active" }
            };

            _mockDbLayer
                .Setup(x => x.GetAllByStatusAsync(null))
                .ReturnsAsync(categories);

            // Act
            var result = await _service.GetAllByStatusAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetAllByStatusAsync_WithEmptyResult_ReturnsEmptyList()
        {
            // Arrange
            _mockDbLayer
                .Setup(x => x.GetAllByStatusAsync("Inactive"))
                .ReturnsAsync(new List<PortalCategory>());

            // Act
            var result = await _service.GetAllByStatusAsync("Inactive");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsCategory()
        {
            // Arrange
            var category = new PortalCategory { Id = 1, Name = "Category 1" };

            _mockDbLayer
                .Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(category);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Category 1", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockDbLayer
                .Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((PortalCategory)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByName_WithValidName_ReturnsMappedCategory()
        {
            // Arrange
            var categories = new List<PortalCategory>
            {
                new PortalCategory { Id = 1, Name = "Electronics" },
                new PortalCategory { Id = 2, Name = "Books" }
            };

            _mockDbLayer
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _service.GetByName("Electronics");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.PortalCategory);
            Assert.Equal("Electronics", result.PortalCategory.Name);
        }

        [Fact]
        public async Task GetByName_WithNonexistentName_ReturnsNull()
        {
            // Arrange
            var categories = new List<PortalCategory>
            {
                new PortalCategory { Id = 1, Name = "Electronics" }
            };

            _mockDbLayer
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _service.GetByName("NonExistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByName_WithEmptyList_ReturnsNull()
        {
            // Arrange
            _mockDbLayer
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<PortalCategory>());

            // Act
            var result = await _service.GetByName("Any");

            // Assert
            Assert.Null(result);
        }
    }
}
