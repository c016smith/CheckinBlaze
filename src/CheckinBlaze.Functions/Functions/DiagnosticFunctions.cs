using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using CheckinBlaze.Functions.Services;
using System.Threading.Tasks;

namespace CheckinBlaze.Functions.Functions
{
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
    }
}