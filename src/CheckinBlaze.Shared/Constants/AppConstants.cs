namespace CheckinBlaze.Shared.Constants
{
    /// <summary>
    /// Application-wide constants
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// Azure Table Storage table names
        /// </summary>
        public static class TableNames
        {
            public const string CheckInRecords = "checkinrecords";
            public const string UserPreferences = "userpreferences";
            public const string AuditLogs = "auditlogs";
            public const string HeadcountCampaigns = "headcountcampaigns";
        }
        
        /// <summary>
        /// Microsoft Graph API endpoints
        /// </summary>
        public static class GraphApi
        {
            public const string BaseUrl = "https://graph.microsoft.com";
            public const string ApiVersion = "v1.0";
            public const string BaseApiUrl = BaseUrl + "/" + ApiVersion;
            
            public const string MeEndpoint = BaseApiUrl + "/me";
            public const string UsersEndpoint = BaseApiUrl + "/users";
            public const string DirectReportsEndpoint = MeEndpoint + "/directReports";
            public const string ManagerEndpoint = MeEndpoint + "/manager";
            public const string PhotoEndpoint = MeEndpoint + "/photo/$value";
            public const string TeamsEndpoint = BaseApiUrl + "/teams";
            public const string ChatsEndpoint = BaseApiUrl + "/chats";
        }
        
        /// <summary>
        /// Azure AD tenant information
        /// </summary>
        public static class AzureAd
        {
            public const string TenantName = "r6jd.onmicrosoft.com";
            public const string TenantId = "e8ce889a-ba5e-4fbc-ae63-d095a9b60d95";
            public const string ClientId = "4b781c1c-0b04-4c3b-ad66-427458e9f98d";
            public const string Instance = "https://login.microsoftonline.com/";
        }
        
        /// <summary>
        /// API scopes required for the application
        /// </summary>
        public static class ApiScopes
        {
            public const string UserRead = "User.Read";
            public const string UserReadBasicAll = "User.ReadBasic.All";
            public const string DirectoryReadAll = "Directory.Read.All";
            public const string TeamsAppInstallation = "TeamsAppInstallation.ReadWriteForUser";
            public const string ChatSendMessage = "Chat.ReadWrite";
            public const string PresenceRead = "Presence.Read";
        }
    }
}