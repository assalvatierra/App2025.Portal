using Moq;
using Portal.Models;
using Portal.Services.VectorSearch;
using Portal.Services.VectorSearch.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Xunit;

namespace Portal.Tests.Services.VectorSearch
{
    public class VectorServiceTests
    {
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly VectorService _service;
        private const string VectorDbCacheKey = "VectorDbScheduleSettings";

        public VectorServiceTests()
        {
            _mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<VectorService>>();
            _service = new VectorService(_mockCache.Object, mockLogger.Object);
        }

        #region GetScheduleSetting Tests

        [Fact]
        public void GetScheduleSetting_WithValidCachedData_ReturnsParsedVectorDbScheduleSettings()
        {
            // Arrange
            var expectedSettings = new VectorDbScheduleSettings
            {
                NextSchedule = new DateTime(2024, 12, 31, 10, 0, 0),
                IntervalValue = 24,
                IntervalUnit = "Hours",
                LastModified = new DateTime(2024, 12, 20, 15, 30, 0),
                LastUpdateTriggered = new DateTime(2024, 12, 21, 10, 0, 0)
            };

            var serializedBytes = JsonSerializer.SerializeToUtf8Bytes(expectedSettings);

            _mockCache
                .Setup(x => x.Get(VectorDbCacheKey))
                .Returns(serializedBytes);

            // Act
            var result = _service.GetScheduleSetting();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedSettings.NextSchedule, result.NextSchedule);
            Assert.Equal(expectedSettings.IntervalValue, result.IntervalValue);
            Assert.Equal(expectedSettings.IntervalUnit, result.IntervalUnit);
            Assert.Equal(expectedSettings.LastModified, result.LastModified);
            Assert.Equal(expectedSettings.LastUpdateTriggered, result.LastUpdateTriggered);

            _mockCache.Verify(x => x.Get(VectorDbCacheKey), Times.Once);
        }

        [Fact]
        public void GetScheduleSetting_WithNullCachedData_ReturnsNull()
        {
            // Arrange
            _mockCache
                .Setup(x => x.Get(VectorDbCacheKey))
                .Returns((byte[]?)null);

            // Act
            var result = _service.GetScheduleSetting();

            // Assert
            Assert.Null(result);
            _mockCache.Verify(x => x.Get(VectorDbCacheKey), Times.Once);
        }

        [Fact]
        public void GetScheduleSetting_WithInvalidCachedData_ReturnsNull()
        {
            // Arrange
            var invalidBytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };

            _mockCache
                .Setup(x => x.Get(VectorDbCacheKey))
                .Returns(invalidBytes);

            // Act
            var result = _service.GetScheduleSetting();

            // Assert
            Assert.Null(result);
            _mockCache.Verify(x => x.Get(VectorDbCacheKey), Times.Once);
        }

        [Fact]
        public void GetScheduleSetting_WhenCacheThrowsException_ReturnsNull()
        {
            // Arrange
            _mockCache
                .Setup(x => x.Get(VectorDbCacheKey))
                .Throws(new Exception("Cache operation failed"));

            // Act
            var result = _service.GetScheduleSetting();

            // Assert
            Assert.Null(result);
            _mockCache.Verify(x => x.Get(VectorDbCacheKey), Times.Once);
        }

        [Fact]
        public void GetScheduleSetting_WithComplexObject_ReturnsParsedVectorDbScheduleSettings()
        {
            // Arrange
            var expectedSettings = new VectorDbScheduleSettings
            {
                NextSchedule = new DateTime(2025, 1, 15, 8, 30, 45),
                IntervalValue = 7,
                IntervalUnit = "Days",
                LastModified = new DateTime(2024, 12, 1, 9, 0, 0),
                LastUpdateTriggered = null
            };

            var serializedBytes = JsonSerializer.SerializeToUtf8Bytes(expectedSettings);

            _mockCache
                .Setup(x => x.Get(VectorDbCacheKey))
                .Returns(serializedBytes);

            // Act
            var result = _service.GetScheduleSetting();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(7, result.IntervalValue);
            Assert.Equal("Days", result.IntervalUnit);
            Assert.Null(result.LastUpdateTriggered);
        }

        #endregion

        #region SetScheduleSetting Tests

        [Fact]
        public void SetScheduleSetting_WithValidSettings_ReturnsOneAndSetsCacheData()
        {
            // Arrange
            var settings = new VectorDbScheduleSettings
            {
                NextSchedule = new DateTime(2024, 12, 31, 10, 0, 0),
                IntervalValue = 24,
                IntervalUnit = "Hours",
                LastModified = new DateTime(2024, 12, 20, 15, 30, 0),
                LastUpdateTriggered = new DateTime(2024, 12, 21, 10, 0, 0)
            };

            // Act
            var result = _service.SetScheduleSetting(settings);

            // Assert
            Assert.Equal(1, result);
            _mockCache.Verify(
                x => x.Set(
                    VectorDbCacheKey,
                    It.Is<byte[]>(b => b != null && b.Length > 0),
                    It.IsAny<DistributedCacheEntryOptions>()),
                Times.Once);
        }

        [Fact]
        public void SetScheduleSetting_WithMinimalSettings_ReturnsOneAndSetsCacheData()
        {
            // Arrange
            var settings = new VectorDbScheduleSettings
            {
                NextSchedule = new DateTime(2025, 1, 1),
                IntervalValue = 1,
                IntervalUnit = "Minutes",
                LastModified = DateTime.UtcNow,
                LastUpdateTriggered = null
            };

            // Act
            var result = _service.SetScheduleSetting(settings);

            // Assert
            Assert.Equal(1, result);
            _mockCache.Verify(
                x => x.Set(
                    VectorDbCacheKey,
                    It.Is<byte[]>(b => b != null && b.Length > 0),
                    It.IsAny<DistributedCacheEntryOptions>()),
                Times.Once);
        }

        [Fact]
        public void SetScheduleSetting_WhenCacheThrowsException_ReturnsZero()
        {
            // Arrange
            var settings = new VectorDbScheduleSettings
            {
                NextSchedule = new DateTime(2024, 12, 31, 10, 0, 0),
                IntervalValue = 24,
                IntervalUnit = "Hours",
                LastModified = DateTime.UtcNow,
                LastUpdateTriggered = null
            };

            _mockCache
                .Setup(x => x.Set(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()))
                .Throws(new Exception("Cache operation failed"));

            // Act
            var result = _service.SetScheduleSetting(settings);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void SetScheduleSetting_PreservesAllSettingsProperties()
        {
            // Arrange
            var settings = new VectorDbScheduleSettings
            {
                NextSchedule = new DateTime(2025, 6, 15, 14, 30, 0),
                IntervalValue = 30,
                IntervalUnit = "Minutes",
                LastModified = new DateTime(2024, 12, 10, 8, 0, 0),
                LastUpdateTriggered = new DateTime(2024, 12, 10, 10, 15, 0)
            };

            byte[]? capturedBytes = null;

            _mockCache
                .Setup(x => x.Set(
                    VectorDbCacheKey,
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>()))
                .Callback<string, byte[], DistributedCacheEntryOptions>((key, bytes, options) =>
                {
                    capturedBytes = bytes;
                });

            // Act
            var result = _service.SetScheduleSetting(settings);

            // Assert
            Assert.Equal(1, result);
            Assert.NotNull(capturedBytes);

            // Verify serialized and deserialized data matches
            var deserializedSettings = JsonSerializer.Deserialize<VectorDbScheduleSettings>(capturedBytes);
            Assert.Equal(settings.NextSchedule, deserializedSettings?.NextSchedule);
            Assert.Equal(settings.IntervalValue, deserializedSettings?.IntervalValue);
            Assert.Equal(settings.IntervalUnit, deserializedSettings?.IntervalUnit);
            Assert.Equal(settings.LastModified, deserializedSettings?.LastModified);
            Assert.Equal(settings.LastUpdateTriggered, deserializedSettings?.LastUpdateTriggered);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void SetScheduleSetting_ThenGetScheduleSetting_ReturnsMatchingData()
        {
            // Arrange
            var originalSettings = new VectorDbScheduleSettings
            {
                NextSchedule = new DateTime(2025, 3, 20, 12, 0, 0),
                IntervalValue = 12,
                IntervalUnit = "Hours",
                LastModified = DateTime.UtcNow,
                LastUpdateTriggered = null
            };

            byte[]? serializedData = null;

            // Setup: Capture the data when Set is called
            _mockCache
                .Setup(x => x.Set(VectorDbCacheKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()))
                .Callback<string, byte[], DistributedCacheEntryOptions>((key, bytes, options) =>
                {
                    serializedData = bytes;
                });

            // Setup: Return captured data when Get is called
            _mockCache
                .Setup(x => x.Get(VectorDbCacheKey))
                .Returns(() => serializedData);

            // Act
            var setResult = _service.SetScheduleSetting(originalSettings);
            var getResult = _service.GetScheduleSetting();

            // Assert
            Assert.Equal(1, setResult);
            Assert.NotNull(getResult);
            Assert.Equal(originalSettings.IntervalValue, getResult.IntervalValue);
            Assert.Equal(originalSettings.IntervalUnit, getResult.IntervalUnit);
            Assert.Equal(originalSettings.NextSchedule, getResult.NextSchedule);
        }

        #endregion
    }
}
