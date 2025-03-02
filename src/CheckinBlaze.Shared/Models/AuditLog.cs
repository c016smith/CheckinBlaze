using System;

namespace CheckinBlaze.Shared.Models
{
    /// <summary>
    /// Represents an audit log entry for tracking changes in the system
    /// </summary>
    public class AuditLog
    {
        /// <summary>
        /// Unique identifier for the audit log entry
        /// </summary>
        public required string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Azure AD user identifier of the user who performed the action
        /// </summary>
        public required string UserId { get; set; }
        
        /// <summary>
        /// Display name of the user who performed the action
        /// </summary>
        public required string UserDisplayName { get; set; }
        
        /// <summary>
        /// Timestamp when the action occurred
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        
        /// <summary>
        /// The type of entity that was affected (e.g., UserPreferences, CheckIn)
        /// </summary>
        public required string EntityType { get; set; }
        
        /// <summary>
        /// The identifier of the entity that was affected
        /// </summary>
        public required string EntityId { get; set; }
        
        /// <summary>
        /// The type of action that was performed
        /// </summary>
        public AuditActionType ActionType { get; set; }
        
        /// <summary>
        /// Description of the changes that were made
        /// </summary>
        public required string ChangeDescription { get; set; }
        
        /// <summary>
        /// Previous state of the entity (if applicable), stored as JSON
        /// </summary>
        public string? PreviousState { get; set; }
        
        /// <summary>
        /// New state of the entity after the change, stored as JSON
        /// </summary>
        public string? NewState { get; set; }
        
        /// <summary>
        /// IP address of the user who performed the action
        /// </summary>
        public string? IpAddress { get; set; }
        
        /// <summary>
        /// User agent string of the browser/client that performed the action
        /// </summary>
        public string? UserAgent { get; set; }
    }
    
    /// <summary>
    /// Defines the types of actions that can be audited
    /// </summary>
    public enum AuditActionType
    {
        /// <summary>
        /// A new entity was created
        /// </summary>
        Create = 0,
        
        /// <summary>
        /// An existing entity was updated
        /// </summary>
        Update = 1,
        
        /// <summary>
        /// An entity was deleted
        /// </summary>
        Delete = 2,
        
        /// <summary>
        /// A user logged in to the application
        /// </summary>
        Login = 3,
        
        /// <summary>
        /// A user logged out of the application
        /// </summary>
        Logout = 4,
        
        /// <summary>
        /// A user submitted a check-in
        /// </summary>
        CheckIn = 5,
        
        /// <summary>
        /// A headcount campaign was initiated
        /// </summary>
        HeadcountInitiated = 6,
        
        /// <summary>
        /// A check-in was acknowledged
        /// </summary>
        CheckInAcknowledged = 7,
        
        /// <summary>
        /// A check-in was resolved
        /// </summary>
        CheckInResolved = 8
    }
}