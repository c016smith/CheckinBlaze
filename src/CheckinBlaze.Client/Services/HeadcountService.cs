using CheckinBlaze.Shared.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CheckinBlaze.Client.Services
{
    /// <summary>
    /// Service for managing headcount campaigns and team notifications
    /// </summary>
    public class HeadcountService
    {
        private readonly HttpClient _httpClient;
        private readonly GraphService _graphService;

        public HeadcountService(IHttpClientFactory httpClientFactory, GraphService graphService)
        {
            _httpClient = httpClientFactory.CreateClient("CheckinBlazeFunctions");
            _graphService = graphService;
        }

        /// <summary>
        /// Create a new headcount campaign for the current user's team
        /// </summary>
        public async Task<HeadcountCampaign> CreateHeadcountCampaignAsync(string title, string description, List<string> targetUserIds = null)
        {
            try
            {
                var currentUser = await _graphService.GetCurrentUserAsync();

                // If no target users specified, get direct reports
                if (targetUserIds == null || targetUserIds.Count == 0)
                {
                    var directReports = await _graphService.GetDirectReportsAsync();
                    targetUserIds = new List<string>();
                    
                    foreach (var user in directReports)
                    {
                        targetUserIds.Add(user.Id);
                    }
                }

                var campaign = new HeadcountCampaign
                {
                    Title = title,
                    Description = description,
                    InitiatedByUserId = currentUser.Id,
                    InitiatedByUPN = currentUser.UserPrincipalName,
                    InitiatedByDisplayName = currentUser.DisplayName,
                    CreatedTimestamp = DateTimeOffset.UtcNow,
                    Status = HeadcountCampaignStatus.Active,
                    TargetedUserIds = targetUserIds
                };

                var response = await _httpClient.PostAsJsonAsync("api/headcount", campaign);
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadFromJsonAsync<HeadcountCampaign>();
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
                return new HeadcountCampaign(); // Replaced null with empty object
            }
            catch (Exception)
            {
                // Handle other exceptions
                throw;
            }
        }

        /// <summary>
        /// Get active headcount campaigns initiated by the current user
        /// </summary>
        public async Task<List<HeadcountCampaign>> GetActiveHeadcountCampaignsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/headcount/active");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<HeadcountCampaign>>();
                }
                return new List<HeadcountCampaign>();
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
                return new List<HeadcountCampaign>();
            }
            catch (Exception)
            {
                // Handle other exceptions
                return new List<HeadcountCampaign>();
            }
        }

        /// <summary>
        /// Get a specific headcount campaign by ID
        /// </summary>
        public async Task<HeadcountCampaign> GetHeadcountCampaignAsync(string campaignId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/headcount/{campaignId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<HeadcountCampaign>();
                }
                return new HeadcountCampaign(); // Replaced null with empty object
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
                return new HeadcountCampaign(); // Replaced null with empty object
            }
            catch (Exception)
            {
                // Handle other exceptions
                return new HeadcountCampaign(); // Replaced null with empty object
            }
        }

        /// <summary>
        /// Update the status of a headcount campaign
        /// </summary>
        public async Task<bool> UpdateCampaignStatusAsync(string campaignId, HeadcountCampaignStatus status)
        {
            try
            {
                var updateModel = new
                {
                    Status = status
                };

                var response = await _httpClient.PutAsJsonAsync($"api/headcount/{campaignId}/status", updateModel);
                return response.IsSuccessStatusCode;
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
                return false;
            }
            catch (Exception)
            {
                // Handle other exceptions
                return false;
            }
        }

        /// <summary>
        /// Send Teams notifications to all targeted users in a campaign
        /// </summary>
        public async Task<bool> SendTeamsNotificationsAsync(string campaignId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/headcount/{campaignId}/notify", null);
                return response.IsSuccessStatusCode;
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
                return false;
            }
            catch (Exception)
            {
                // Handle other exceptions
                return false;
            }
        }

        /// <summary>
        /// Get all check-ins associated with a specific headcount campaign
        /// </summary>
        public async Task<List<CheckInRecord>> GetCampaignCheckInsAsync(string campaignId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/headcount/{campaignId}/checkins");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<CheckInRecord>>();
                }
                return new List<CheckInRecord>();
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
                return new List<CheckInRecord>();
            }
            catch (Exception)
            {
                // Handle other exceptions
                return new List<CheckInRecord>();
            }
        }
    }
}