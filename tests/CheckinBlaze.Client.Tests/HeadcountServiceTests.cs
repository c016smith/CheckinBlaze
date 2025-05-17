using CheckinBlaze.Client.Services;
using CheckinBlaze.Shared.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Moq;
using Moq.Protected;
using System.Net;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CheckinBlaze.Client.Tests
{
    public class HeadcountServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly Mock<GraphService> _mockGraphService;
        private readonly HeadcountService _headcountService;

        public HeadcountServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://localhost")
            };

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory
                .Setup(factory => factory.CreateClient("CheckinBlazeFunctions"))
                .Returns(_httpClient);

            _mockGraphService = new Mock<GraphService>();
            _headcountService = new HeadcountService(mockHttpClientFactory.Object, _mockGraphService.Object);
        }

        [Fact]
        public async Task CreateHeadcountCampaignAsync_ShouldReturnCampaign_WhenSuccessful()
        {
            // Arrange
            var title = "Test Campaign";
            var description = "Test Description";
            var targetUserIds = new List<string> { "user1", "user2" };

            var currentUser = new Microsoft.Graph.Models.User
            {
                Id = "testuser",
                DisplayName = "Test User"
            };

            _mockGraphService
                .Setup(service => service.GetCurrentUserAsync())
                .ReturnsAsync(currentUser);

            var campaignResponse = new HeadcountCampaign
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Description = description,
                InitiatedByUserId = currentUser.Id,
                InitiatedByDisplayName = currentUser.DisplayName,
                TargetedUserIds = targetUserIds
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = new StringContent(JsonSerializer.Serialize(campaignResponse))
                });

            // Act
            var result = await _headcountService.CreateHeadcountCampaignAsync(title, description, targetUserIds);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(title, result.Title);
            Assert.Equal(description, result.Description);
            Assert.Equal(currentUser.Id, result.InitiatedByUserId);
            Assert.Equal(currentUser.DisplayName, result.InitiatedByDisplayName);
            Assert.Equal(targetUserIds, result.TargetedUserIds);
        }

        [Fact]
        public async Task CreateHeadcountCampaignAsync_ShouldFetchDirectReports_WhenTargetUsersNotProvided()
        {
            // Arrange
            var title = "Test Campaign";
            var description = "Test Description";

            var currentUser = new Microsoft.Graph.Models.User
            {
                Id = "testuser",
                DisplayName = "Test User",
                UserPrincipalName = "test@contoso.com"
            };

            var directReports = new List<Microsoft.Graph.Models.User>
            {
                new Microsoft.Graph.Models.User { Id = "report1" },
                new Microsoft.Graph.Models.User { Id = "report2" }
            };

            var expectedTargetIds = directReports.Select(u => u.Id).ToList();

            _mockGraphService.Setup(s => s.GetCurrentUserAsync()).ReturnsAsync(currentUser);
            _mockGraphService.Setup(s => s.GetDirectReportsAsync()).ReturnsAsync(directReports);

            var campaignResponse = new HeadcountCampaign
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Description = description,
                InitiatedByUserId = currentUser.Id,
                InitiatedByDisplayName = currentUser.DisplayName,
                TargetedUserIds = expectedTargetIds
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = new StringContent(JsonSerializer.Serialize(campaignResponse))
                });

            // Act
            var result = await _headcountService.CreateHeadcountCampaignAsync(title, description);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedTargetIds, result.TargetedUserIds);
            _mockGraphService.Verify(s => s.GetDirectReportsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetActiveHeadcountCampaignsAsync_ShouldReturnCampaigns_WhenSuccessful()
        {
            // Arrange
            var campaigns = new List<HeadcountCampaign>
            {
                new HeadcountCampaign
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Campaign 1",
                    Description = "Description 1",
                    InitiatedByUserId = "user1",
                    Status = HeadcountCampaignStatus.Active
                },
                new HeadcountCampaign
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Campaign 2",
                    Description = "Description 2",
                    InitiatedByUserId = "user1",
                    Status = HeadcountCampaignStatus.Active
                }
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(campaigns))
                });

            // Act
            var result = await _headcountService.GetActiveHeadcountCampaignsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, c => Assert.Equal(HeadcountCampaignStatus.Active, c.Status));
        }

        [Fact]
        public async Task GetActiveHeadcountCampaignsAsync_ShouldReturnEmptyList_WhenTokenException()
        {
            // Arrange
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new AccessTokenNotAvailableException(async (r) => { }, null, null, null));

            // Act
            var result = await _headcountService.GetActiveHeadcountCampaignsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}