using CheckinBlaze.Functions.Services;
using CheckinBlaze.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace CheckinBlaze.Functions.Functions
{
    /// <summary>
    /// Functions for user preference operations
    /// </summary>
    public class UserPreferenceFunctions
    {
        private readonly ILogger _logger;
        private readonly UserPreferenceService _userPreferenceService;

        public UserPreferenceFunctions(
            ILoggerFactory loggerFactory,
            UserPreferenceService userPreferenceService)
        {
            _logger = loggerFactory.CreateLogger<UserPreferenceFunctions>();
            _userPreferenceService = userPreferenceService;
        }

        /// <summary>
        /// Get user preferences for the authenticated user
        /// </summary>
        [Function("GetUserPreferences")]
        public async Task<HttpResponseData> GetUserPreferences(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "preferences")] HttpRequestData req,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation("Getting user preferences");

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
                // Get user preferences (creates default if none exist)
                var preferences = await _userPreferenceService.GetUserPreferencesAsync(userId);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(preferences);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error getting user preferences");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error getting user preferences: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Update user preferences for the authenticated user
        /// </summary>
        [Function("UpdateUserPreferences")]
        public async Task<HttpResponseData> UpdateUserPreferences(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "preferences")] HttpRequestData req,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation("Updating user preferences");

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
                // Parse the preferences from the request body
                var requestBody = await req.ReadAsStringAsync();
                var preferences = JsonSerializer.Deserialize<UserPreferences>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Make sure the preferences are for the authenticated user
                preferences.UserId = userId;

                // Save the preferences
                var updatedPreferences = await _userPreferenceService.SaveUserPreferencesAsync(preferences, userId);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(updatedPreferences);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error updating user preferences: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Get user preferences for a specific user (admin operation)
        /// </summary>
        [Function("GetUserPreferencesById")]
        public async Task<HttpResponseData> GetUserPreferencesById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "preferences/{targetUserId}")] HttpRequestData req,
            string targetUserId,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation($"Getting user preferences for user {targetUserId}");

            // Verify the user is authenticated
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            // Get the requesting user ID from the claims
            var requestingUserId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isAdmin = claimsPrincipal.IsInRole("Admin");

            // Only allow if the user is an admin or if they are requesting their own preferences
            if (!isAdmin && requestingUserId != targetUserId)
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                await forbiddenResponse.WriteStringAsync("You are not authorized to access this user's preferences");
                return forbiddenResponse;
            }

            try
            {
                // Get user preferences for the specified user
                var preferences = await _userPreferenceService.GetUserPreferencesAsync(targetUserId);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(preferences);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error getting user preferences for user {targetUserId}");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error getting user preferences: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Update user preferences for a specific user (admin operation)
        /// </summary>
        [Function("UpdateUserPreferencesById")]
        public async Task<HttpResponseData> UpdateUserPreferencesById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "preferences/{targetUserId}")] HttpRequestData req,
            string targetUserId,
            ClaimsPrincipal claimsPrincipal)
        {
            _logger.LogInformation($"Updating user preferences for user {targetUserId}");

            // Verify the user is authenticated
            if (!claimsPrincipal.Identity.IsAuthenticated)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            // Get the requesting user ID from the claims
            var requestingUserId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isAdmin = claimsPrincipal.IsInRole("Admin");

            // Only allow if the user is an admin or if they are updating their own preferences
            if (!isAdmin && requestingUserId != targetUserId)
            {
                var forbiddenResponse = req.CreateResponse(HttpStatusCode.Forbidden);
                await forbiddenResponse.WriteStringAsync("You are not authorized to update this user's preferences");
                return forbiddenResponse;
            }

            try
            {
                // Parse the preferences from the request body
                var requestBody = await req.ReadAsStringAsync();
                var preferences = JsonSerializer.Deserialize<UserPreferences>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Make sure the preferences are for the specified user
                preferences.UserId = targetUserId;

                // Save the preferences
                var updatedPreferences = await _userPreferenceService.SaveUserPreferencesAsync(preferences, requestingUserId);

                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(updatedPreferences);
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"Error updating user preferences for user {targetUserId}");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Error updating user preferences: {ex.Message}");
                return errorResponse;
            }
        }
    }
}