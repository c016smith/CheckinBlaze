using Azure;
using Azure.Data.Tables;
using CheckinBlaze.Functions.Services;
using CheckinBlaze.Shared.Models;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CheckinBlaze.Functions.Tests
{
    public class CheckInServiceTests
    {
        private readonly Mock<TableClient> _mockCheckInTable;
        private readonly Mock<AuditService> _mockAuditService;
        private readonly CheckInService _checkInService;

        public CheckInServiceTests()
        {
            _mockCheckInTable = new Mock<TableClient>();
            _mockAuditService = new Mock<AuditService>(Mock.Of<TableClient>());
            _checkInService = new CheckInService(_mockCheckInTable.Object, _mockAuditService.Object);
        }

        [Fact]
        public async Task CreateCheckInAsync_ShouldCreateAndAudit()
        {
            // Arrange
            var checkIn = new CheckInRecord
            {
                UserId = "user1",
                UserDisplayName = "Test User",
                UserEmail = "test@example.com",
                Timestamp = DateTimeOffset.UtcNow,
                Latitude = 47.6062,
                Longitude = -122.3321,
                LocationPrecision = LocationPrecision.Precise,
                Status = SafetyStatus.OK,
                Notes = "Test check-in",
                State = CheckInState.Submitted
            };

            // Act
            var result = await _checkInService.CreateCheckInAsync(checkIn, "user1");

            // Assert
            _mockCheckInTable.Verify(t => t.AddEntityAsync(
                It.Is<Models.CheckInEntity>(e => 
                    e.PartitionKey == checkIn.UserId && 
                    e.UserId == checkIn.UserId &&
                    e.UserDisplayName == checkIn.UserDisplayName &&
                    e.Status == checkIn.Status.ToString()),
                It.IsAny<CancellationToken>()));

            _mockAuditService.Verify(a => a.LogActionAsync(
                checkIn.UserId,
                checkIn.UserDisplayName,
                AuditActionType.CheckIn,
                "CheckInRecord",
                result.Id,
                null,
                It.IsAny<string>(),
                null,
                null));
        }

        [Fact]
        public async Task AcknowledgeCheckInAsync_ShouldUpdateStateAndAudit()
        {
            // Arrange
            var checkInId = Guid.NewGuid().ToString();
            var userId = "user1";
            var acknowledgedBy = "admin1";
            var existingCheckIn = new Models.CheckInEntity
            {
                PartitionKey = userId,
                RowKey = checkInId,
                UserId = userId,
                UserDisplayName = "Test User",
                Status = SafetyStatus.NeedsAssistance.ToString(),
                State = CheckInState.Submitted.ToString()
            };

            _mockCheckInTable.Setup(t => t.GetEntityIfExistsAsync<Models.CheckInEntity>(
                userId, checkInId, null, default))
                .ReturnsAsync(Response.FromValue(existingCheckIn, Mock.Of<Response>()));

            // Act
            await _checkInService.AcknowledgeCheckInAsync(userId, checkInId, acknowledgedBy, "Admin User");

            // Assert
            _mockCheckInTable.Verify(t => t.UpdateEntityAsync(
                It.Is<Models.CheckInEntity>(e => 
                    e.State == CheckInState.Acknowledged.ToString() &&
                    e.AcknowledgedByUserId == acknowledgedBy),
                It.IsAny<ETag>(),
                It.IsAny<CancellationToken>()));

            _mockAuditService.Verify(a => a.LogActionAsync(
                acknowledgedBy,
                "System",
                AuditActionType.Update,
                "CheckInRecord",
                checkInId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                null));
        }

        [Fact]
        public async Task ResolveCheckInAsync_ShouldUpdateStateAndAudit()
        {
            // Arrange
            var checkInId = Guid.NewGuid().ToString();
            var userId = "user1";
            var resolvedBy = "admin1";
            var existingCheckIn = new Models.CheckInEntity
            {
                PartitionKey = userId,
                RowKey = checkInId,
                UserId = userId,
                UserDisplayName = "Test User",
                Status = SafetyStatus.NeedsAssistance.ToString(),
                State = CheckInState.Acknowledged.ToString()
            };

            _mockCheckInTable.Setup(t => t.GetEntityIfExistsAsync<Models.CheckInEntity>(
                userId, checkInId, null, default))
                .ReturnsAsync(Response.FromValue(existingCheckIn, Mock.Of<Response>()));

            // Act
            await _checkInService.ResolveCheckInAsync(userId, checkInId, resolvedBy, "Admin User");

            // Assert
            _mockCheckInTable.Verify(t => t.UpdateEntityAsync(
                It.Is<Models.CheckInEntity>(e => 
                    e.State == CheckInState.Resolved.ToString() &&
                    e.ResolvedByUserId == resolvedBy),
                It.IsAny<ETag>(),
                It.IsAny<CancellationToken>()));

            _mockAuditService.Verify(a => a.LogActionAsync(
                resolvedBy,
                "System",
                AuditActionType.Update,
                "CheckInRecord",
                checkInId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                null));
        }
    }
}