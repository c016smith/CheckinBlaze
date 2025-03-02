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
                
                // Set up OpenID Connect configuration
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
                    // Removed RoleClaimType to avoid unnecessary role validation
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
                // Get the Authorization header from the request
                var httpRequestData = await context.GetHttpRequestDataAsync();
                if (httpRequestData != null && httpRequestData.Headers.TryGetValues("Authorization", out var authHeaderValues))
                {
                    var authHeader = authHeaderValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Found Bearer token in request");
                        var token = authHeader.Substring("Bearer ".Length).Trim();
                        
                        // Get the OpenID Connect configuration
                        var openIdConfig = await _configManager.GetConfigurationAsync(CancellationToken.None);
                        
                        // Update validation parameters with signing keys
                        var validationParams = _validationParameters.Clone();
                        validationParams.IssuerSigningKeys = openIdConfig.SigningKeys;
                        
                        try
                        {
                            // Validate the token
                            var handler = new JwtSecurityTokenHandler();
                            var claimsPrincipal = handler.ValidateToken(token, validationParams, out var validatedToken);
                            
                            // Log the claims for debugging
                            _logger.LogInformation($"JWT validated. Claims: {string.Join(", ", claimsPrincipal.Claims.Select(c => $"{c.Type}={c.Value}"))}");
                            
                            // Create a ClaimsIdentity with proper authentication (no role claim type)
                            var identity = new ClaimsIdentity(claimsPrincipal.Claims, "Bearer", _validationParameters.NameClaimType, null);
                            var authenticatedPrincipal = new ClaimsPrincipal(identity);
                            
                            // Store the claims principal in the context items
                            context.Items["ClaimsPrincipal"] = authenticatedPrincipal;
                            _logger.LogInformation("JWT token validated successfully");
                        }
                        catch (SecurityTokenException ex)
                        {
                            _logger.LogWarning(ex, "Security token validation failed");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Invalid Authorization header format: {AuthHeader}", 
                            string.IsNullOrEmpty(authHeader) ? "(empty)" : authHeader);
                    }
                }
                else
                {
                    _logger.LogWarning("No Authorization header found");
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