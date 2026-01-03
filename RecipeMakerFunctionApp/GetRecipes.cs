using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System.Net;

namespace RecipeMakerFunctionApp;

public class GetRecipes
{
    private readonly ILogger<GetRecipes> _logger;

    public GetRecipes(ILogger<GetRecipes> logger)
    {
        _logger = logger;
    }

    [Function("GetRecipes")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
        [TableInput("Recipes", Connection = "AzureWebJobsStorage")] TableClient tableClient,
        FunctionContext executionContext)
    {
        try
        {
            string? userId = null;

#if DEBUG
            //local testing of userid as the header won't conta
            userId = "local-chef-123";
            _logger.LogInformation("DEBUG MODE: Using mock user ID: {userId}", userId);
#else
            
            //grab user ID from the token, x-ms-client-principlal-id is the "sub" value of the decoded token
            if (req.Headers.TryGetValues("X-MS-CLIENT-PRINCIPAL-ID", out var principalIds))
            {
                userId = principalIds.FirstOrDefault();
            }
#endif

            // Validation: If no ID is found, return 401.
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized: Identity header missing. Check Authentication settings.");
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            // Perform the query using the validated User ID as the PartitionKey.
            var recipePage = tableClient.Query<RecipeEntity>(filter: $"PartitionKey eq '{userId}'");
            var recipeList = recipePage.ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);

            // Standard performance optimization: Cache for 5 minutes.
            //response.Headers.Add("Cache-Control", "private, max-age=300");

            await response.WriteAsJsonAsync(recipeList);
            return response;
        }
        catch (Exception ex)
        {
            // The "Safer Way": Log the details but give the user a generic ID.
            var correlationId = Guid.NewGuid().ToString();

            // This ensures the full Stack Trace is searchable in App Insights by ID.
            _logger.LogCritical(ex, "GetRecipes CRASHED. CorrelationId: {CorrelationId}", correlationId);

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);

#if DEBUG
            // In VS 2022, you see the full error immediately.
            await errorResponse.WriteStringAsync($"DEBUG ERROR: {ex.Message} \n\n {ex.StackTrace}");
#else
            // In Production, the user sees a secure reference ID.
            await errorResponse.WriteStringAsync($"An internal error occurred. Ref: {correlationId}");
#endif
            return errorResponse;
        }
    }
}