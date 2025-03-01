using CheckinBlaze.Functions.Services;
using CheckinBlaze.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using Microsoft.Identity.Abstractions;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace CheckinBlaze.Functions.Functions
{
    /// <summary>
    /// Functions for check-in operations
    /// </summary>
    public class CheckInFunctions
    {
        private readonly ILogger _logger;
        private readonly CheckInService _checkInService;
        private readonly IDownstreamApi _downstreamApi;

        public CheckInFunctions(
            ILoggerFactory loggerFactory,
            CheckInService checkInService,
            IDownstreamApi downstreamApi)
        {
            _logger = loggerFactory.CreateLogger<CheckInFunctions>();
            _checkInService = checkInService;
            _downstreamApi = downstreamApi;
        }

        /// <summary>
        /// Create a new check-in
        /// </summary>
        [Function("CreateCheckIn")]
        public async Task<HttpResponseData> CreateCheckIn(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "checkins")] HttpRequestData req,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation("Creating new check-in");

            // Verify the user is authenticated
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            // Get the user ID from the claims
            var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userDisplayName = claimsPrincipal.FindFirst("name")?.Value ?? "Unknown User";

            try
            {
                // Parse the check-in from the request body
                var requestBody = await req.ReadAsStringAsync();
                var checkIn = JsonSerializer.Deserialize<CheckInRecord>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Create the check-in
                var createdCheckIn = await _checkInService.CreateCheckInAsync(checkIn, userId);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.Created);
                await response.WriteAsJsonAsync(createdCheckIn);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error creating check-in");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error creating check-in: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Get the latest check-in for the authenticated user
        /// </summary>
        [Function("GetLatestCheckIn")]
        public async Task<HttpResponseData> GetLatestCheckIn(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "checkins/latest")] HttpRequestData req,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation("Getting latest check-in");

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
                // Get the latest check-in
                var latestCheckIn = await _checkInService.GetLatestCheckInAsync(userId);

                // If no check-in was found, return a 404
                if (latestCheckIn == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync("No check-ins found for the user");
                    return notFoundResponse;
                }

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(latestCheckIn);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error getting latest check-in");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error getting latest check-in: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Get check-in history for the authenticated user
        /// </summary>
        [Function("GetCheckInHistory")]
        public async Task<HttpResponseData> GetCheckInHistory(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "checkins/history")] HttpRequestData req,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation("Getting check-in history");

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
                // Parse max results from query string
                int maxResults = 50; // Default value
                string maxResultsStr = req.Query["maxResults"];
                if (!string.IsNullOrEmpty(maxResultsStr))
                {
                    int.TryParse(maxResultsStr, out maxResults);
                }

                // Get check-in history
                var history = await _checkInService.GetCheckInHistoryAsync(userId, maxResults);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(history);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error getting check-in history");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error getting check-in history: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Update an existing check-in
        /// </summary>
        [Function("UpdateCheckIn")]
        public async Task<HttpResponseData> UpdateCheckIn(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "checkins/{checkInId}")] HttpRequestData req,
            string checkInId,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation($"Updating check-in {checkInId}");

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
                // Parse the update from the request body
                var requestBody = await req.ReadAsStringAsync();
                var updateData = JsonSerializer.Deserialize<CheckInRecord>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Make sure the ID in the URL matches the ID in the body
                updateData.Id = checkInId;
                updateData.UserId = userId; // Only allow updating your own check-ins

                // Update the check-in
                var updatedCheckIn = await _checkInService.UpdateCheckInAsync(updateData, userId);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(updatedCheckIn);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error updating check-in {checkInId}");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error updating check-in: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Acknowledge a check-in that needs assistance
        /// </summary>
        [Function("AcknowledgeCheckIn")]
        public async Task<HttpResponseData> AcknowledgeCheckIn(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "checkins/{userId}/{checkInId}/acknowledge")] HttpRequestData req,
            string userId,
            string checkInId,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation($"Acknowledging check-in {checkInId}");

            // Verify the user is authenticated
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            // Get the acknowledging user ID from the claims
            var acknowledgingUserId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var acknowledgingUserName = claimsPrincipal.FindFirst("name")?.Value ?? "Unknown User";

            try
            {
                // Acknowledge the check-in
                var acknowledgedCheckIn = await _checkInService.AcknowledgeCheckInAsync(
                    userId, checkInId, acknowledgingUserId, acknowledgingUserName);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(acknowledgedCheckIn);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error acknowledging check-in {checkInId}");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error acknowledging check-in: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Resolve a check-in that has been acknowledged
        /// </summary>
        [Function("ResolveCheckIn")]
        public async Task<HttpResponseData> ResolveCheckIn(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "checkins/{userId}/{checkInId}/resolve")] HttpRequestData req,
            string userId,
            string checkInId,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation($"Resolving check-in {checkInId}");

            // Verify the user is authenticated
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            // Get the resolving user ID from the claims
            var resolvingUserId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var resolvingUserName = claimsPrincipal.FindFirst("name")?.Value ?? "Unknown User";

            try
            {
                // Resolve the check-in
                var resolvedCheckIn = await _checkInService.ResolveCheckInAsync(
                    userId, checkInId, resolvingUserId, resolvingUserName);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(resolvedCheckIn);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error resolving check-in {checkInId}");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error resolving check-in: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Get check-ins that need assistance
        /// </summary>
        [Function("GetNeedsAssistanceCheckIns")]
        public async Task<HttpResponseData> GetNeedsAssistanceCheckIns(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "checkins/needsassistance")] HttpRequestData req,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation("Getting check-ins that need assistance");

            // Verify the user is authenticated
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            try
            {
                // Get check-ins that need assistance
                var checkIns = await _checkInService.GetNeedsAssistanceCheckInsAsync();

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(checkIns);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error getting check-ins that need assistance");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error getting check-ins that need assistance: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Get check-ins for a specific user
        /// </summary>
        [Function("GetUserCheckIns")]
        public async Task<HttpResponseData> GetUserCheckIns(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "checkins/user/{userId}")] HttpRequestData req,
            string userId,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation($"Getting check-ins for user {userId}");

            // Verify the user is authenticated
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            try
            {
                // Parse max results from query string
                int maxResults = 50; // Default value
                string maxResultsStr = req.Query["maxResults"];
                if (!string.IsNullOrEmpty(maxResultsStr))
                {
                    int.TryParse(maxResultsStr, out maxResults);
                }

                // Get check-ins for the specified user
                var checkIns = await _checkInService.GetCheckInHistoryAsync(userId, maxResults);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(checkIns);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error getting check-ins for user {userId}");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error getting check-ins for user {userId}: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Test endpoint to verify storage connectivity
        /// </summary>
        [Function("TestStorage")]
        public async Task<HttpResponseData> TestStorage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test/storage")] HttpRequestData req)
        {
            _logger.LogInformation("Testing storage connectivity");
            try
            {
                // Test storage by attempting to create a test record
                var testResult = await _checkInService.TestStorageConnectionAsync();
                
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { Status = "Success", Message = testResult });
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Storage connectivity test failed");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Storage test failed: {ex.Message}");
                return errorResponse;
            }
        }
    }
}