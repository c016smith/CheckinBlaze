using Azure.Data.Tables;
using CheckinBlaze.Functions.Services;
using CheckinBlaze.Shared.Models;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CheckinBlaze.Functions.Tests
{
    public class AuditServiceTests
    {
        private readonly Mock<TableClient> _mockAuditTable;
        private readonly AuditService _auditService;

        public AuditServiceTests()
        {
            _mockAuditTable = new Mock<TableClient>();
            _auditService = new AuditService(_mockAuditTable.Object);
        }

        [Fact]
        public async Task LogActionAsync_ShouldCreateAuditRecord()
        {
            // Arrange
            var userId = "user1";
            var displayName = "Test User";
            var actionType = AuditActionType.CheckIn;
            var entityType = "CheckInRecord";
            var entityId = Guid.NewGuid().ToString();
            var previousState = "{}";
            var newState = "{\"status\":\"OK\"}";

            // Act
            await _auditService.LogActionAsync(
                userId,
                displayName,
                actionType,
                entityType,
                entityId,
                previousState,
                newState);

            // Assert
            _mockAuditTable.Verify(t => t.AddEntityAsync(
                It.Is<TableEntity>(e => 
                    e.PartitionKey == entityType &&
                    e.GetString("UserId") == userId &&
                    e.GetString("UserDisplayName") == displayName &&
                    e.GetString("ActionType") == actionType.ToString() &&
                    e.GetString("EntityType") == entityType &&
                    e.GetString("EntityId") == entityId &&
                    e.GetString("PreviousState") == previousState &&
                    e.GetString("NewState") == newState &&
                    e.GetString("ChangeDescription") == "User submitted a check-in"),
                It.IsAny<CancellationToken>()));
        }

        [Theory]
        [InlineData(AuditActionType.Create, "CheckInRecord", "Created new CheckInRecord")]
        [InlineData(AuditActionType.Update, "CheckInRecord", "Updated CheckInRecord")]
        [InlineData(AuditActionType.CheckInAcknowledged, "CheckInRecord", "Check-in acknowledged")]
        [InlineData(AuditActionType.CheckInResolved, "CheckInRecord", "Check-in resolved")]
        public async Task LogActionAsync_ShouldUseCorrectChangeDescription(
            AuditActionType actionType, 
            string entityType, 
            string expectedDescription)
        {
            // Arrange
            var userId = "user1";
            var displayName = "Test User";
            var entityId = Guid.NewGuid().ToString();

            // Act
            await _auditService.LogActionAsync(
                userId,
                displayName,
                actionType,
                entityType,
                entityId);

            // Assert
            _mockAuditTable.Verify(t => t.AddEntityAsync(
                It.Is<TableEntity>(e => 
                    e.GetString("ChangeDescription") == expectedDescription),
                It.IsAny<CancellationToken>()));
        }
    }
}