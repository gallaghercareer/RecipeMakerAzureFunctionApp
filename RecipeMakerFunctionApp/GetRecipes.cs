using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;
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
     // Note: We are still using TableClient, but we will query using <RecipeEntity>
     [TableInput("Recipes", Connection = "AzureWebJobsStorage")] TableClient tableClient,
     FunctionContext executionContext)
    {
        try
        {
            string? userId = null;

            //testing for local
#if DEBUG
            // 1. Local Testing Mode (Visual Studio 2022 F5)
            // This bypasses the need for a real login token so you can test your logic instantly.
            userId = "local-chef-123";
            _logger.LogInformation("DEBUG MODE: Using mock user ID: {userId}", userId);
#else

                var user = executionContext.Features.Get<ClaimsPrincipal>();
                 userId = user?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
#endif

            if (string.IsNullOrEmpty(userId)) return req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);

            // STRONG TYPED QUERY: Using <RecipeEntity> here is the magic part
            var recipePage = tableClient.Query<RecipeEntity>(filter: $"PartitionKey eq '{userId}'");

            // Convert the page to a list so it's easier for the frontend to read
            var recipeList = recipePage.ToList();


            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);

            // 'private' = only for this user. 'max-age=300' = 5 minutes.
            //The user 'refreshing' the page won't spam the Azure function. Reduces cost.
            response.Headers.Add("Cache-Control", "private, max-age=300");

            await response.WriteAsJsonAsync(recipeList);

            return response;
        }
      


           catch (Exception ex)
        {
            // 1. Generate a unique ID for this specific crash
            var correlationId = Guid.NewGuid().ToString();

            // 2. Log EVERYTHING to Azure securely. 
            // This includes the stack trace, the correlation ID, and the exception.
            _logger.LogCritical(ex, "GetRecipes CRASHED. CorrelationId: {CorrelationId}", correlationId);

            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);

            // 3. Different responses based on environment
#if DEBUG
            // Locally, keep the detail for fast fixing
            await errorResponse.WriteStringAsync($"DEBUG ERROR: {ex.Message} \n\n {ex.StackTrace}");
#else
    // In Production, send a safe message with the ID
    await errorResponse.WriteStringAsync($"An internal error occurred. Error ID: {correlationId}");
#endif

            return errorResponse;
        }
    }
    }
}