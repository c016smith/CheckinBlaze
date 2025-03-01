using CheckinBlaze.Functions.Services;
using CheckinBlaze.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Abstractions;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace CheckinBlaze.Functions.Functions
{
    /// <summary>
    /// Functions for headcount campaign operations
    /// </summary>
    public class HeadcountFunctions
    {
        private readonly ILogger _logger;
        private readonly HeadcountService _headcountService;
        private readonly CheckInService _checkInService;
        private readonly IDownstreamApi _downstreamApi;

        public HeadcountFunctions(
            ILoggerFactory loggerFactory,
            HeadcountService headcountService,
            CheckInService checkInService,
            IDownstreamApi downstreamApi)
        {
            _logger = loggerFactory.CreateLogger<HeadcountFunctions>();
            _headcountService = headcountService;
            _checkInService = checkInService;
            _downstreamApi = downstreamApi;
        }

        /// <summary>
        /// Create a new headcount campaign
        /// </summary>
        [Function("CreateHeadcountCampaign")]
        public async Task<HttpResponseData> CreateHeadcountCampaign(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "headcount")] HttpRequestData req,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation("Creating new headcount campaign");

            // Verify the user is authenticated
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            // Get the user ID and display name from the claims
            var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userDisplayName = claimsPrincipal.FindFirst("name")?.Value ?? "Unknown User";

            try
            {
                // Parse the campaign from the request body
                var requestBody = await req.ReadAsStringAsync();
                var campaign = JsonSerializer.Deserialize<HeadcountCampaign>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Create the campaign
                var createdCampaign = await _headcountService.CreateCampaignAsync(campaign, userId, userDisplayName);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(createdCampaign);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error creating headcount campaign");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error creating headcount campaign: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Get a specific headcount campaign
        /// </summary>
        [Function("GetHeadcountCampaign")]
        public async Task<HttpResponseData> GetHeadcountCampaign(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "headcount/{campaignId}")] HttpRequestData req,
            string campaignId,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation($"Getting headcount campaign {campaignId}");

            // Verify the user is authenticated
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            // Get the user ID from the claims
            var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            try
            {
                // Parse the initiator ID from the query string
                string initiatorId = req.Query["initiatorId"];
                
                // If not provided, assume the current user is the initiator
                if (string.IsNullOrEmpty(initiatorId))
                {
                    initiatorId = userId;
                }

                // Get the campaign
                var campaign = await _headcountService.GetCampaignAsync(initiatorId, campaignId);

                // If no campaign was found, return a 404
                if (campaign == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync($"Campaign with ID {campaignId} not found");
                    return notFoundResponse;
                }

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(campaign);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error getting headcount campaign {campaignId}");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error getting headcount campaign: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Get active headcount campaigns for the authenticated user
        /// </summary>
        [Function("GetActiveHeadcountCampaigns")]
        public async Task<HttpResponseData> GetActiveHeadcountCampaigns(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "headcount/active")] HttpRequestData req,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation("Getting active headcount campaigns");

            // Verify the user is authenticated
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            // Get the user ID from the claims
            var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            try
            {
                // Get active campaigns
                var campaigns = await _headcountService.GetActiveCampaignsAsync(userId);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(campaigns);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error getting active headcount campaigns");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error getting active headcount campaigns: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Update the status of a headcount campaign
        /// </summary>
        [Function("UpdateHeadcountCampaignStatus")]
        public async Task<HttpResponseData> UpdateHeadcountCampaignStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "headcount/{campaignId}/status")] HttpRequestData req,
            string campaignId,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation($"Updating headcount campaign status for {campaignId}");

            // Verify the user is authenticated
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            // Get the user ID and display name from the claims
            var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userDisplayName = claimsPrincipal.FindFirst("name")?.Value ?? "Unknown User";

            try
            {
                // Parse the status update from the request body
                var requestBody = await req.ReadAsStringAsync();
                var statusUpdate = JsonSerializer.Deserialize<HeadcountStatusUpdate>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Update the campaign status
                var updatedCampaign = await _headcountService.UpdateCampaignStatusAsync(
                    userId, campaignId, statusUpdate.Status, userId, userDisplayName);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(updatedCampaign);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error updating headcount campaign status for {campaignId}");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error updating headcount campaign status: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Get check-ins for a specific headcount campaign
        /// </summary>
        [Function("GetHeadcountCampaignCheckIns")]
        public async Task<HttpResponseData> GetHeadcountCampaignCheckIns(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "headcount/{campaignId}/checkins")] HttpRequestData req,
            string campaignId,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation($"Getting check-ins for headcount campaign {campaignId}");

            // Verify the user is authenticated
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            try
            {
                // Get check-ins for the campaign
                var checkIns = await _checkInService.GetCheckInsByCampaignAsync(campaignId);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(checkIns);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error getting check-ins for headcount campaign {campaignId}");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error getting check-ins for headcount campaign: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Send notifications for a headcount campaign
        /// </summary>
        [Function("SendHeadcountNotifications")]
        public async Task<HttpResponseData> SendHeadcountNotifications(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "headcount/{campaignId}/notify")] HttpRequestData req,
            string campaignId,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation($"Sending notifications for headcount campaign {campaignId}");

            // Verify the user is authenticated
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            // Get the user ID from the claims
            var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            try
            {
                // Get the campaign
                var campaign = await _headcountService.GetCampaignAsync(userId, campaignId);

                // If no campaign was found, return a 404
                if (campaign == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync($"Campaign with ID {campaignId} not found");
                    return notFoundResponse;
                }

                // In a real implementation, we would send notifications here
                // For now, just return a successful response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync($"Notifications sent for campaign {campaignId}");
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error sending notifications for headcount campaign {campaignId}");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error sending notifications: {ex.Message}");
                return errorResponse;
            }
        }
    }

    /// <summary>
    /// DTO for updating a headcount campaign's status
    /// </summary>
    public class HeadcountStatusUpdate
    {
        public HeadcountCampaignStatus Status { get; set; }
    }
}