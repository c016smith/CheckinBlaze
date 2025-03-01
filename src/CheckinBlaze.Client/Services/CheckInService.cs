using CheckinBlaze.Shared.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

namespace CheckinBlaze.Client.Services
{
    /// <summary>
    /// Service for handling check-in operations and communication with the backend API
    /// </summary>
    public class CheckInService
    {
        private readonly HttpClient _httpClient;
        private readonly GraphService _graphService;
        private readonly ILogger<CheckInService> _logger;

        public CheckInService(IHttpClientFactory httpClientFactory, GraphService graphService, ILogger<CheckInService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("CheckinBlazeFunctions");
            _graphService = graphService;
            _logger = logger;
        }

        /// <summary>
        /// Submit a new check-in record
        /// </summary>
        public async Task<CheckInRecord> SubmitCheckInAsync(double? latitude, double? longitude, 
            string? notes, SafetyStatus status, LocationPrecision precision)
        {
            try
            {
                _logger.LogInformation("Submitting new check-in");
                var currentUser = await _graphService.GetCurrentUserAsync();
                
                var checkIn = new CheckInRecord
                {
                    UserId = currentUser?.Id,
                    UserDisplayName = currentUser?.DisplayName,
                    UserEmail = currentUser?.Mail ?? currentUser?.UserPrincipalName,
                    UserDepartment = currentUser?.Department,
                    UserJobTitle = currentUser?.JobTitle,
                    UserOfficeLocation = currentUser?.OfficeLocation,
                    Latitude = latitude,
                    Longitude = longitude,
                    Notes = notes,
                    Status = status,
                    LocationPrecision = precision,
                    Timestamp = DateTimeOffset.UtcNow,
                    State = CheckInState.Submitted
                };

                _logger.LogInformation($"Calling API: POST api/checkins for user {checkIn.UserDisplayName}");
                var response = await _httpClient.PostAsJsonAsync("api/checkins", checkIn);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API error: {response.StatusCode} - {errorContent}");
                    throw new HttpRequestException($"Error submitting check-in: {response.StatusCode} - {errorContent}");
                }
                
                _logger.LogInformation("Check-in submitted successfully");
                return await response.Content.ReadFromJsonAsync<CheckInRecord>();
            }
            catch (AccessTokenNotAvailableException ex)
            {
                _logger.LogWarning("Access token not available, redirecting to authentication");
                ex.Redirect();
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting check-in");
                throw;
            }
        }

        /// <summary>
        /// Get the most recent check-in for the current user
        /// </summary>
        public async Task<CheckInRecord?> GetLatestCheckInAsync()
        {
            try
            {
                _logger.LogInformation("Getting latest check-in");
                var response = await _httpClient.GetAsync("api/checkins/latest");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Latest check-in retrieved successfully");
                    return await response.Content.ReadFromJsonAsync<CheckInRecord>();
                }
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("No check-ins found for the current user");
                    return null;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"API error: {response.StatusCode} - {errorContent}");
                throw new HttpRequestException($"Error getting latest check-in: {response.StatusCode} - {errorContent}");
            }
            catch (AccessTokenNotAvailableException ex)
            {
                _logger.LogWarning("Access token not available, redirecting to authentication");
                ex.Redirect();
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest check-in");
                throw;
            }
        }

        /// <summary>
        /// Get check-in history for the current user
        /// </summary>
        public async Task<List<CheckInRecord>> GetCheckInHistoryAsync()
        {
            try
            {
                _logger.LogInformation("Getting check-in history");
                var response = await _httpClient.GetAsync("api/checkins/history");
                
                if (response.IsSuccessStatusCode)
                {
                    var history = await response.Content.ReadFromJsonAsync<List<CheckInRecord>>();
                    _logger.LogInformation($"Retrieved {history?.Count ?? 0} check-in history items");
                    return history ?? new List<CheckInRecord>();
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"API error: {response.StatusCode} - {errorContent}");
                throw new HttpRequestException($"Error getting check-in history: {response.StatusCode} - {errorContent}");
            }
            catch (AccessTokenNotAvailableException ex)
            {
                _logger.LogWarning("Access token not available, redirecting to authentication");
                ex.Redirect();
                return new List<CheckInRecord>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving check-in history");
                throw;
            }
        }

        /// <summary>
        /// Update an existing check-in with additional information
        /// </summary>
        public async Task<bool> UpdateCheckInAsync(string checkInId, string notes, SafetyStatus status)
        {
            try
            {
                var updateModel = new
                {
                    Id = checkInId,
                    Notes = notes,
                    Status = status
                };

                var response = await _httpClient.PutAsJsonAsync($"api/checkins/{checkInId}", updateModel);
                return response.IsSuccessStatusCode;
            }
            catch (AccessTokenNotAvailableException ex)
            {
                _logger.LogWarning("Access token not available, redirecting to authentication");
                ex.Redirect();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating check-in");
                return false;
            }
        }

        /// <summary>
        /// Get check-ins for a specific user (requires appropriate permissions)
        /// </summary>
        public async Task<List<CheckInRecord>> GetUserCheckInsAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/checkins/user/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<CheckInRecord>>();
                }
                return new List<CheckInRecord>();
            }
            catch (AccessTokenNotAvailableException ex)
            {
                _logger.LogWarning("Access token not available, redirecting to authentication");
                ex.Redirect();
                return new List<CheckInRecord>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user check-ins");
                return new List<CheckInRecord>();
            }
        }

        /// <summary>
        /// Acknowledge a check-in that needs assistance
        /// </summary>
        public async Task<bool> AcknowledgeCheckInAsync(string checkInId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/checkins/{checkInId}/acknowledge", null);
                return response.IsSuccessStatusCode;
            }
            catch (AccessTokenNotAvailableException ex)
            {
                _logger.LogWarning("Access token not available, redirecting to authentication");
                ex.Redirect();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging check-in");
                return false;
            }
        }

        /// <summary>
        /// Resolve a check-in that has been acknowledged
        /// </summary>
        public async Task<bool> ResolveCheckInAsync(string checkInId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/checkins/{checkInId}/resolve", null);
                return response.IsSuccessStatusCode;
            }
            catch (AccessTokenNotAvailableException ex)
            {
                _logger.LogWarning("Access token not available, redirecting to authentication");
                ex.Redirect();
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving check-in");
                return false;
            }
        }
    }
}