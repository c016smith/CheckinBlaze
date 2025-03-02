using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using CheckinBlaze.Functions.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using CheckinBlaze.Shared.Models;

namespace CheckinBlaze.Functions.Functions
{
    /// <summary>
    /// Diagnostic functions for testing and debugging
    /// </summary>
    public class DiagnosticFunctions
    {
        private readonly ILogger _logger;
        private readonly CheckInService _checkInService;

        public DiagnosticFunctions(
            ILoggerFactory loggerFactory,
            CheckInService checkInService)
        {
            _logger = loggerFactory.CreateLogger<DiagnosticFunctions>();
            _checkInService = checkInService;
        }

        /// <summary>
        /// Test endpoint to verify storage connectivity
        /// </summary>
        [Function("TestStorage")]
        public async Task<HttpResponseData> TestStorage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test/storage")] HttpRequestData req,
            FunctionContext context)
        {
            _logger.LogInformation("Testing storage connectivity");
            try
            {
                // Test storage by attempting to create a test record
                var testResult = await _checkInService.TestStorageConnectionAsync();
                
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { Status = "Success", Message = testResult });
                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Storage connectivity test failed");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Storage test failed: {ex.Message}");
                return errorResponse;
            }
        }

        /// <summary>
        /// Test endpoint to check if the Functions host is running
        /// </summary>
        [Function("DiagnosticPing")]
        public HttpResponseData Ping(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "diagnostic/ping")] HttpRequestData req)
        {
            _logger.LogInformation("Diagnostic ping received");
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString("Functions host is running!");
            return response;
        }
        
        /// <summary>
        /// Test endpoint to retrieve check-ins from storage without authentication
        /// FOR TESTING PURPOSES ONLY - DO NOT USE IN PRODUCTION
        /// </summary>
        [Function("GetCheckInsTest")]
        public async Task<HttpResponseData> GetCheckInsTest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "diagnostic/checkins")] HttpRequestData req)
        {
            _logger.LogWarning("DIAGNOSTIC ENDPOINT: Accessing check-in data without authentication");
            
            try
            {
                // Parse the query parameters
                int maxResults = 10; // Default limit
                string maxResultsStr = req.Query["limit"];
                if (!string.IsNullOrEmpty(maxResultsStr) && int.TryParse(maxResultsStr, out int parsedLimit))
                {
                    maxResults = Math.Min(parsedLimit, 100); // Cap at 100 for performance
                }
                
                // Get all check-ins
                _logger.LogInformation($"Retrieving up to {maxResults} recent check-ins for diagnostic purposes");
                var checkIns = await _checkInService.GetRecentCheckInsAsync(maxResults);
                
                // Create the response
                var response = req.CreateResponse(HttpStatusCode.OK);
                
                // Always return a valid JSON array even if empty
                if (checkIns == null || checkIns.Count == 0)
                {
                    _logger.LogInformation("No check-ins found, returning empty array");
                    await response.WriteAsJsonAsync(new List<CheckInRecord>());
                }
                else
                {
                    _logger.LogInformation($"Retrieved {checkIns.Count} check-ins");
                    await response.WriteAsJsonAsync(checkIns);
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving check-ins for diagnostic test");
                
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new 
                { 
                    error = "Error retrieving check-ins",
                    details = ex.Message,
                    stackTrace = ex.StackTrace
                });
                return errorResponse;
            }
        }
    }
}