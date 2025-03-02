using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using CheckinBlaze.Functions.Services;
using System.Threading.Tasks;
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
    }
}