using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CheckinBlaze.Client.Services
{
    /// <summary>
    /// Service for interacting with Microsoft Graph API
    /// </summary>
    public class GraphService
    {
        private readonly GraphServiceClient _graphClient;

        public GraphService(GraphServiceClient graphClient)
        {
            _graphClient = graphClient;
        }

        /// <summary>
        /// Get the current user's profile information
        /// </summary>
        public async Task<User> GetCurrentUserAsync()
        {
            try
            {
                return await _graphClient.Me.GetAsync();
            }
            catch (ServiceException)
            {
                // Handle Graph API exceptions
                throw;
            }
        }

        /// <summary>
        /// Get the user's profile photo as a base64 string
        /// </summary>
        public async Task<string> GetUserPhotoAsync()
        {
            try
            {
                var photoStream = await _graphClient.Me.Photo.Content.GetAsync();
                if (photoStream != null)
                {
                    using var memoryStream = new MemoryStream();
                    await photoStream.CopyToAsync(memoryStream);
                    var photoBytes = memoryStream.ToArray();
                    return $"data:image/jpeg;base64,{Convert.ToBase64String(photoBytes)}";
                }
                return null;
            }
            catch (ServiceException)
            {
                // Photo might not exist or user might not have permission
                return null;
            }
        }

        /// <summary>
        /// Get the current user's direct reports
        /// </summary>
        public async Task<List<User>> GetDirectReportsAsync()
        {
            try
            {
                var directReports = await _graphClient.Me.DirectReports.GetAsync();
                var users = new List<User>();
                
                if (directReports?.Value != null)
                {
                    foreach (var directReport in directReports.Value)
                    {
                        if (directReport is User user)
                        {
                            users.Add(user);
                        }
                    }
                }
                
                return users;
            }
            catch (ServiceException)
            {
                // Handle Graph API exceptions
                return new List<User>();
            }
        }

        /// <summary>
        /// Get the current user's manager
        /// </summary>
        public async Task<User> GetManagerAsync()
        {
            try
            {
                var manager = await _graphClient.Me.Manager.GetAsync();
                return manager as User;
            }
            catch (ServiceException)
            {
                // Handle Graph API exceptions or no manager case
                return null;
            }
        }

        /// <summary>
        /// Search for users in the organization
        /// </summary>
        public async Task<List<User>> SearchUsersAsync(string searchText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 3)
                {
                    return new List<User>();
                }

                var users = await _graphClient.Users.GetAsync(requestConfig => 
                {
                    requestConfig.QueryParameters.Search = $"\"displayName:{searchText}\" OR \"mail:{searchText}\"";
                    requestConfig.QueryParameters.Select = new[] { "id", "displayName", "mail", "userPrincipalName", "jobTitle", "department" };
                    requestConfig.QueryParameters.Top = 10;
                });
                
                return users?.Value?.ToList() ?? new List<User>();
            }
            catch (ServiceException)
            {
                // Handle Graph API exceptions
                return new List<User>();
            }
        }

        /// <summary>
        /// Send a Teams chat notification to a user
        /// </summary>
        public async Task<bool> SendTeamsNotificationAsync(string userId, string messageContent)
        {
            try
            {
                // Implementation will depend on the specific Teams API approach
                // This is a placeholder for the Teams integration
                // Actual implementation would use Graph API to send a chat message
                // first 'placeholder' code I'm super happy didn't make major assumptions :) 
                
                return true;
            }
            catch (ServiceException)
            {
                return false;
            }
        }
    }
}