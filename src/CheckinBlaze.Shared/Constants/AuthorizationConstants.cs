using System;

namespace CheckinBlaze.Shared.Constants
{
    /// <summary>
    /// Authorization-related constants for the application
    /// </summary>
    public static class AuthorizationConstants
    {
        /// <summary>
        /// Application policies
        /// </summary>
        public static class Policies
        {
            /// <summary>
            /// Policy requiring authenticated users
            /// </summary>
            public const string AuthenticatedUser = "AuthenticatedUserPolicy";
        }

        /// <summary>
        /// API scopes for authorization
        /// </summary>
        public static class Scopes
        {
            /// <summary>
            /// Main application scope
            /// </summary>
            public const string AppAccess = "api://4b781c1c-0b04-4c3b-ad66-427458e9f98d/user_impersonation";
        }
    }
}