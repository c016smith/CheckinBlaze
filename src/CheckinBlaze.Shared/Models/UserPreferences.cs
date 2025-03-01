using System;

namespace CheckinBlaze.Shared.Models
{
    /// <summary>
    /// Stores user preferences for the check-in application
    /// </summary>
    public class UserPreferences
    {
        /// <summary>
        /// Azure AD user identifier
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Default location precision setting for check-ins
        /// </summary>
        public LocationPrecision DefaultLocationPrecision { get; set; } = LocationPrecision.CityWide;
        
        /// <summary>
        /// Whether to enable location services by default
        /// </summary>
        public bool EnableLocationServices { get; set; } = true;
        
        /// <summary>
        /// Whether to receive Teams notifications for headcount campaigns
        /// </summary>
        public bool EnableTeamsNotifications { get; set; } = true;
        
        /// <summary>
        /// Last modified timestamp for auditing purposes
        /// </summary>
        public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
        
        /// <summary>
        /// User ID of the last person to modify these preferences
        /// </summary>
        public string LastModifiedBy { get; set; }
    }
}