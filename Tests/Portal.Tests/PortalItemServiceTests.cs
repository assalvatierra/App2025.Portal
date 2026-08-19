using Xunit;
using Moq;
using Erp.Domain.Models;
using Portal.DBLayer;
using Portal.DBServices;
using Portal.Models;

namespace Portal.Tests.DBServices
{
    public class PortalItemServiceTests
    {
        private readonly Mock<IPortalItemDbLayer> _mockDbLayer;
        private readonly Mock<IPortalItemSpecDbLayer> _mockItemSpecsDbLayer;
        private readonly PortalItemService _service;

        public PortalItemServiceTests()
        {
            _mockDbLayer = new Mock<IPortalItemDbLayer>();
            _mockItemSpecsDbLayer = new Mock<IPortalItemSpecDbLayer>();
            _service = new PortalItemService(_mockDbLayer.Object, _mockItemSpecsDbLayer.Object);
        }

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllItems()
        {
            // Arrange
            var expectedItems = new List<PortalItem>
            {
                new PortalItem { Id = 1, Name = "Item 1", CreatedBy = "Test", LastEditBy = "Test" },
                new PortalItem { Id = 2, Name = "Item 2", CreatedBy = "Test", LastEditBy = "Test" }
            };
            _mockDbLayer.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedItems);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Item 1", result[0].Name);
            Assert.Equal("Item 2", result[1].Name);
            _mockDbLayer.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyListWhenNoItems()
        {
            // Arrange
            _mockDbLayer.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<PortalItem>());

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockDbLayer.Verify(x => x.GetAllAsync(), Times.Once);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnItem()
        {
            // Arrange
            int itemId = 1;
            var expectedItem = new PortalItem { Id = itemId, Name = "Test Item", CreatedBy = "Test", LastEditBy = "Test" };
            _mockDbLayer.Setup(x => x.GetByIdAsync(itemId)).ReturnsAsync(expectedItem);

            // Act
            var result = await _service.GetByIdAsync(itemId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(itemId, result.Id);
            Assert.Equal("Test Item", result.Name);
            _mockDbLayer.Verify(x => x.GetByIdAsync(itemId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            int itemId = 999;
            _mockDbLayer.Setup(x => x.GetByIdAsync(itemId)).ReturnsAsync((PortalItem?)null);

            // Act
            var result = await _service.GetByIdAsync(itemId);

            // Assert
            Assert.Null(result);
            _mockDbLayer.Verify(x => x.GetByIdAsync(itemId), Times.Once);
        }

        #endregion

        #region GetByIdListAsync Tests

        [Fact]
        public async Task GetByIdListAsync_WithValidIds_ShouldReturnDtoList()
        {
            // Arrange
            var idList = new List<int> { 1, 2, 3 };
            var items = new List<PortalItem>
            {
                new PortalItem { Id = 1, Name = "Item 1", CreatedBy = "Test", LastEditBy = "Test" },
                new PortalItem { Id = 2, Name = "Item 2", CreatedBy = "Test", LastEditBy = "Test" },
                new PortalItem { Id = 3, Name = "Item 3", CreatedBy = "Test", LastEditBy = "Test" }
            };
            _mockDbLayer.Setup(x => x.GetByIdListAsync(idList)).ReturnsAsync(items);

            // Act
            var result = await _service.GetByIdListAsync(idList);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            _mockDbLayer.Verify(x => x.GetByIdListAsync(idList), Times.Once);
        }

        [Fact]
        public async Task GetByIdListAsync_WithEmptyList_ShouldReturnEmptyDtoList()
        {
            // Arrange
            var idList = new List<int>();
            _mockDbLayer.Setup(x => x.GetByIdListAsync(idList)).ReturnsAsync(new List<PortalItem>());

            // Act
            var result = await _service.GetByIdListAsync(idList);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockDbLayer.Verify(x => x.GetByIdListAsync(idList), Times.Once);
        }

        #endregion

        #region GetItemsByCategory Tests

        [Fact]
        public async Task GetItemsByCategory_WithValidCategory_ShouldReturnFilteredItems()
        {
            // Arrange
            string category = "Electronics";
            string type = "Phone";
            var items = new List<PortalItem>
            {
                new PortalItem { Id = 1, Name = "Item 1", CreatedBy = "Test", LastEditBy = "Test" }
            };
            _mockDbLayer.Setup(x => x.GetItemsByCategoryAsync(category, type)).ReturnsAsync(items);

            // Act
            var result = await _service.GetItemsByCategory(category, type);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Count);
            _mockDbLayer.Verify(x => x.GetItemsByCategoryAsync(category, type), Times.Once);
        }

        [Fact]
        public async Task GetItemsByCategory_WithNullType_ShouldReturnCategoryItems()
        {
            // Arrange
            string category = "Electronics";
            var items = new List<PortalItem>
            {
                new PortalItem { Id = 1, Name = "Item 1", CreatedBy = "Test", LastEditBy = "Test" },
                new PortalItem { Id = 2, Name = "Item 2", CreatedBy = "Test", LastEditBy = "Test" }
            };
            _mockDbLayer.Setup(x => x.GetItemsByCategoryAsync(category, null)).ReturnsAsync(items);

            // Act
            var result = await _service.GetItemsByCategory(category, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockDbLayer.Verify(x => x.GetItemsByCategoryAsync(category, null), Times.Once);
        }

        [Fact]
        public async Task GetItemsByCategory_WithNoResults_ShouldReturnEmptyList()
        {
            // Arrange
            string category = "NonExistent";
            _mockDbLayer.Setup(x => x.GetItemsByCategoryAsync(category, null)).ReturnsAsync(new List<PortalItem>());

            // Act
            var result = await _service.GetItemsByCategory(category, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region SearchItemsAsync Tests

        [Fact]
        public async Task SearchItemsAsync_WithValidSearchDto_ShouldReturnMatchingItems()
        {
            // Arrange
            var searchDto = new SearchDto { searchTerm = "test" };
            var specs = new List<PortalItemSpec>
            {
                new PortalItemSpec { Id = 1, PortalItemId = 1 },
                new PortalItemSpec { Id = 2, PortalItemId = 2 }
            };
            var items = new List<PortalItem>
            {
                new PortalItem { Id = 1, Name = "Test Item 1", CreatedBy = "Test", LastEditBy = "Test" },
                new PortalItem { Id = 2, Name = "Test Item 2", CreatedBy = "Test", LastEditBy = "Test" }
            };

            _mockItemSpecsDbLayer.Setup(x => x.GetItemISpecsCriteriaAsync(searchDto)).ReturnsAsync(specs);
            _mockDbLayer.Setup(x => x.SearchItemsAsync("test", It.IsAny<List<int>>())).ReturnsAsync(items);

            // Act
            var result = await _service.SearchItemsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockItemSpecsDbLayer.Verify(x => x.GetItemISpecsCriteriaAsync(searchDto), Times.Once);
            _mockDbLayer.Verify(x => x.SearchItemsAsync("test", It.IsAny<List<int>>()), Times.Once);
        }

        [Fact]
        public async Task SearchItemsAsync_WithNoMatches_ShouldReturnEmptyList()
        {
            // Arrange
            var searchDto = new SearchDto { searchTerm = "nonexistent" };
            _mockItemSpecsDbLayer.Setup(x => x.GetItemISpecsCriteriaAsync(searchDto))
                .ReturnsAsync(new List<PortalItemSpec>());
            _mockDbLayer.Setup(x => x.SearchItemsAsync("nonexistent", It.IsAny<List<int>>()))
                .ReturnsAsync(new List<PortalItem>());

            // Act
            var result = await _service.SearchItemsAsync(searchDto);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_WithValidItem_ShouldSetTimestampsAndGuid()
        {
            // Arrange
            var newItem = new PortalItem { Name = "New Item", CreatedBy = "Test", LastEditBy = "Test" };
            var addedItem = new PortalItem { Id = 1, Name = "New Item", CreatedBy = "Test", LastEditBy = "Test" };
            _mockDbLayer.Setup(x => x.AddAsync(It.IsAny<PortalItem>())).ReturnsAsync(addedItem);

            // Act
            var result = await _service.AddAsync(newItem);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Item", result.Name);
            _mockDbLayer.Verify(x => x.AddAsync(It.IsAny<PortalItem>()), Times.Once);
        }

        [Fact]
        public async Task AddAsync_ShouldSetCreatedOnToUtcNow()
        {
            // Arrange
            PortalItem capturedItem = null;
            var newItem = new PortalItem { Name = "New Item", CreatedBy = "Test", LastEditBy = "Test" };
            _mockDbLayer.Setup(x => x.AddAsync(It.IsAny<PortalItem>()))
                .Callback<PortalItem>(item => capturedItem = item)
                .ReturnsAsync(newItem);

            // Act
            var utcBefore = DateTime.UtcNow;
            await _service.AddAsync(newItem);
            var utcAfter = DateTime.UtcNow;

            // Assert
            Assert.NotNull(capturedItem);
            Assert.True(capturedItem.CreatedOn >= utcBefore && capturedItem.CreatedOn <= utcAfter);
            Assert.True(capturedItem.LastEditOn >= utcBefore && capturedItem.LastEditOn <= utcAfter);
            Assert.NotEqual(Guid.Empty, capturedItem.RecordGuid);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ShouldUpdateLastEditOnToUtcNow()
        {
            // Arrange
            PortalItem capturedItem = null;
            var itemToUpdate = new PortalItem { Id = 1, Name = "Updated Item", CreatedBy = "Test", LastEditBy = "Test", CreatedOn = DateTime.UtcNow.AddDays(-1) };
            _mockDbLayer.Setup(x => x.UpdateAsync(It.IsAny<PortalItem>()))
                .Callback<PortalItem>(item => capturedItem = item)
                .Returns(Task.CompletedTask);

            // Act
            var utcBefore = DateTime.UtcNow;
            await _service.UpdateAsync(itemToUpdate);
            var utcAfter = DateTime.UtcNow;

            // Assert
            Assert.NotNull(capturedItem);
            Assert.True(capturedItem.LastEditOn >= utcBefore && capturedItem.LastEditOn <= utcAfter);
            _mockDbLayer.Verify(x => x.UpdateAsync(It.IsAny<PortalItem>()), Times.Once);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidItem_ShouldCallDbLayer()
        {
            // Arrange
            var itemToDelete = new PortalItem { Id = 1, Name = "Item to Delete", CreatedBy = "Test", LastEditBy = "Test" };
            _mockDbLayer.Setup(x => x.DeleteAsync(itemToDelete)).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteAsync(itemToDelete);

            // Assert
            _mockDbLayer.Verify(x => x.DeleteAsync(itemToDelete), Times.Once);
        }

        #endregion

        #region Exists Tests

        [Fact]
        public void Exists_WithExistingId_ShouldReturnTrue()
        {
            // Arrange
            int itemId = 1;
            _mockDbLayer.Setup(x => x.Exists(itemId)).Returns(true);

            // Act
            var result = _service.Exists(itemId);

            // Assert
            Assert.True(result);
            _mockDbLayer.Verify(x => x.Exists(itemId), Times.Once);
        }

        [Fact]
        public void Exists_WithNonExistingId_ShouldReturnFalse()
        {
            // Arrange
            int itemId = 999;
            _mockDbLayer.Setup(x => x.Exists(itemId)).Returns(false);

            // Act
            var result = _service.Exists(itemId);

            // Assert
            Assert.False(result);
            _mockDbLayer.Verify(x => x.Exists(itemId), Times.Once);
        }

        #endregion
    }
}
