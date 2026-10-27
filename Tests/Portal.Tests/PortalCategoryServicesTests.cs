using Moq;
using Portal.DBLayer;
using Portal.DBServices;
using Erp.Domain.Models;
using Portal.Models;
using Portal.Services;
using Xunit;

namespace Portal.Tests.DBServices
{
    public class PortalCategoryServicesTests
    {
        private readonly Mock<IPortalCategoryDbLayer> _mockDbLayer;
        private readonly Mock<ICache> _mockCache;
        private readonly PortalCategoryServices _service;

        public PortalCategoryServicesTests()
        {
            _mockDbLayer = new Mock<IPortalCategoryDbLayer>();
            _mockCache = new Mock<ICache>();
            _service = new PortalCategoryServices(_mockDbLayer.Object, _mockCache.Object);
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

            _mockCache
                .Setup(x => x.GetAsync<List<ItemCategoryDTO>>(It.IsAny<string>()))
                .ReturnsAsync((List<ItemCategoryDTO>?)null);

            _mockDbLayer
                .Setup(x => x.GetAllByStatusAsync("Active"))
                .ReturnsAsync(categories);

            _mockCache
                .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<List<ItemCategoryDTO>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetAllByStatusAsync("Active");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockDbLayer.Verify(x => x.GetAllByStatusAsync("Active"), Times.Once);
            _mockCache.Verify(x => x.GetAsync<List<ItemCategoryDTO>>(It.IsAny<string>()), Times.Once);
            _mockCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<List<ItemCategoryDTO>>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task GetAllByStatusAsync_WithValidStatus_UsesCachedResult_OnSecondCall()
        {
            // Arrange
            var categories = new List<PortalCategory>
            {
                new PortalCategory { Id = 1, Name = "Category 1", Status = "Active" }
            };

            var cachedResult = new List<ItemCategoryDTO>();
            _mockCache
                .Setup(x => x.GetAsync<List<ItemCategoryDTO>>("categories_status_Active"))
                .ReturnsAsync(cachedResult);

            // Act
            var result = await _service.GetAllByStatusAsync("Active");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cachedResult, result);
            _mockDbLayer.Verify(x => x.GetAllByStatusAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAllByStatusAsync_WithNullStatus_ReturnsMappedCategories()
        {
            // Arrange
            var categories = new List<PortalCategory>
            {
                new PortalCategory { Id = 1, Name = "Category 1", Status = "Active" }
            };

            _mockCache
                .Setup(x => x.GetAsync<List<ItemCategoryDTO>>(It.IsAny<string>()))
                .ReturnsAsync((List<ItemCategoryDTO>?)null);

            _mockDbLayer
                .Setup(x => x.GetAllByStatusAsync(null))
                .ReturnsAsync(categories);

            _mockCache
                .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<List<ItemCategoryDTO>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetAllByStatusAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            _mockDbLayer.Verify(x => x.GetAllByStatusAsync(null), Times.Once);
        }

        [Fact]
        public async Task GetAllByStatusAsync_WithEmptyResult_ReturnsEmptyList()
        {
            // Arrange
            _mockCache
                .Setup(x => x.GetAsync<List<ItemCategoryDTO>>(It.IsAny<string>()))
                .ReturnsAsync((List<ItemCategoryDTO>?)null);

            _mockDbLayer
                .Setup(x => x.GetAllByStatusAsync("Inactive"))
                .ReturnsAsync(new List<PortalCategory>());

            _mockCache
                .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<List<ItemCategoryDTO>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetAllByStatusAsync("Inactive");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            _mockDbLayer
                .Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((PortalCategory?)null);

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
            _mockDbLayer.Verify(x => x.GetAllAsync(), Times.Once);
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

        [Fact]
        public async Task GetCategoriesByTypeAsync_WithValidType_ReturnsMappedCategories()
        {
            // Arrange
            var categories = new List<PortalCategory>
            {
                new PortalCategory { Id = 1, Name = "Type1 Category" },
                new PortalCategory { Id = 2, Name = "Type1 Category 2" }
            };

            _mockCache
                .Setup(x => x.GetAsync<List<ItemCategoryDTO>>(It.IsAny<string>()))
                .ReturnsAsync((List<ItemCategoryDTO>?)null);

            _mockDbLayer
                .Setup(x => x.GetByTypeAsync("Type1"))
                .ReturnsAsync(categories);

            _mockCache
                .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<List<ItemCategoryDTO>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetCategoriesByTypeAsync("Type1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockDbLayer.Verify(x => x.GetByTypeAsync("Type1"), Times.Once);
            _mockCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<List<ItemCategoryDTO>>(), It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task GetCategoriesByTypeAsync_WithCachedData_SkipsDbCall()
        {
            // Arrange
            var cachedResult = new List<ItemCategoryDTO>();
            _mockCache
                .Setup(x => x.GetAsync<List<ItemCategoryDTO>>("categories_type_Type1"))
                .ReturnsAsync(cachedResult);

            // Act
            var result = await _service.GetCategoriesByTypeAsync("Type1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cachedResult, result);
            _mockDbLayer.Verify(x => x.GetByTypeAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetCategoriesByTypeAsync_WithEmptyResult_ReturnsEmptyList()
        {
            // Arrange
            _mockCache
                .Setup(x => x.GetAsync<List<ItemCategoryDTO>>(It.IsAny<string>()))
                .ReturnsAsync((List<ItemCategoryDTO>?)null);

            _mockDbLayer
                .Setup(x => x.GetByTypeAsync("NonexistentType"))
                .ReturnsAsync(new List<PortalCategory>());

            _mockCache
                .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<List<ItemCategoryDTO>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.GetCategoriesByTypeAsync("NonexistentType");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
