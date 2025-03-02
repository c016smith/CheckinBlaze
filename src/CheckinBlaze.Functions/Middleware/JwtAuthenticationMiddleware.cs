using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace CheckinBlaze.Functions.Middleware
{
    public class JwtAuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<JwtAuthenticationMiddleware> _logger;
        private readonly TokenValidationParameters _validationParameters;
        private readonly IConfigurationManager<OpenIdConnectConfiguration> _configManager;
        private readonly bool _isAuthEnabled;

        public JwtAuthenticationMiddleware(IConfiguration configuration, ILogger<JwtAuthenticationMiddleware> logger)
        {
            _logger = logger;
            
            var tenantId = configuration["AzureAd:TenantId"];
            var clientId = configuration["AzureAd:ClientId"];
            _isAuthEnabled = !string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(clientId);
            
            if (_isAuthEnabled)
            {
                _logger.LogInformation($"Initializing JWT Authentication with TenantId: {tenantId} and ClientId: {clientId}");
                
                var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
                var metadataAddress = $"{authority}/.well-known/openid-configuration";
                
                _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    metadataAddress,
                    new OpenIdConnectConfigurationRetriever(),
                    new HttpDocumentRetriever());
                
                _validationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudiences = new[] { $"api://{clientId}", clientId },
                    ValidateIssuer = true,
                    ValidIssuer = $"https://sts.windows.net/{tenantId}/",
                    ValidateLifetime = true,
                    NameClaimType = "name",
                    RoleClaimType = "roles" // Adding role claim type back but won't require specific roles
                };
            }
            else
            {
                _logger.LogWarning("Azure AD authentication is not configured. Set AzureAd:TenantId and AzureAd:ClientId in configuration.");
                _configManager = null;
                _validationParameters = null;
            }
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            if (!_isAuthEnabled)
            {
                _logger.LogWarning("Authentication is disabled. Proceeding without authentication.");
                await next(context);
                return;
            }

            try
            {
                var httpRequestData = await context.GetHttpRequestDataAsync();
                if (httpRequestData != null && httpRequestData.Headers.TryGetValues("Authorization", out var authHeaderValues))
                {
                    var authHeader = authHeaderValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = authHeader.Substring("Bearer ".Length).Trim();
                        var openIdConfig = await _configManager.GetConfigurationAsync(CancellationToken.None);
                        var validationParams = _validationParameters.Clone();
                        validationParams.IssuerSigningKeys = openIdConfig.SigningKeys;
                        
                        try
                        {
                            var handler = new JwtSecurityTokenHandler();
                            var claimsPrincipal = handler.ValidateToken(token, validationParams, out var validatedToken);
                            
                            // Create a new identity with just the essential claims
                            var claims = new List<Claim>();
                            
                            // Add required claims
                            var nameIdentifier = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
                            var name = claimsPrincipal.FindFirst("name");
                            var email = claimsPrincipal.FindFirst(ClaimTypes.Email) ?? claimsPrincipal.FindFirst("upn");
                            
                            if (nameIdentifier != null) claims.Add(nameIdentifier);
                            if (name != null) claims.Add(name);
                            if (email != null) claims.Add(email);
                            
                            // Add any roles if they exist, but don't require them
                            var roles = claimsPrincipal.FindAll(ClaimTypes.Role);
                            claims.AddRange(roles);
                            
                            // Create identity and principal
                            var identity = new ClaimsIdentity(claims, "Bearer", "name", ClaimTypes.Role);
                            var authenticatedPrincipal = new ClaimsPrincipal(identity);
                            
                            context.Items["ClaimsPrincipal"] = authenticatedPrincipal;
                            _logger.LogInformation($"JWT token validated for user {name?.Value ?? "Unknown"}");
                        }
                        catch (SecurityTokenException ex)
                        {
                            _logger.LogWarning(ex, "Security token validation failed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token validation");
            }
            
            await next(context);
        }
    }
}