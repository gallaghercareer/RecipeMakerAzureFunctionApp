using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System.Net;
using RecipeMakerFunctionApp.Models; // Ensure your namespace for entities is included

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
            userId = "local-chef-123";
            _logger.LogInformation("DEBUG MODE: Using mock user ID: {userId}", userId);
#else
            if (req.Headers.TryGetValues("X-MS-CLIENT-PRINCIPAL-ID", out var principalIds))
            {
                userId = principalIds.FirstOrDefault();
            }
#endif

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unauthorized: Identity header missing.");
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            // 1. Query for the generic TableEntity to capture all varying columns
            var entities = tableClient.Query<TableEntity>(filter: $"PartitionKey eq '{userId}'");

            // 2. Prepare lists for each entity type
            var recipeList = new List<RecipeEntity>();
            var groceryLists = new List<GroceriesEntity>();

            foreach (var entity in entities)
            {
                // Use EntityType to decide which class to instantiate
                // Note: .ToLower() helps avoid casing issues (e.g., "recipe" vs "Recipe")
                string type = entity.GetString("EntityType")?.ToLower() ?? "recipe";

                if (type == "grocerylist")
                {
                    groceryLists.Add(new GroceriesEntity
                    {
                        PartitionKey = entity.PartitionKey,
                        RowKey = entity.RowKey,
                        Timestamp = entity.Timestamp,
                        ETag = entity.ETag,
                        EntityType = "GroceryList",
                        Items = entity.GetString("Items") ?? "[]"
                    });
                }
                else
                {
                    recipeList.Add(new RecipeEntity
                    {
                        PartitionKey = entity.PartitionKey,
                        RowKey = entity.RowKey,
                        Timestamp = entity.Timestamp,
                        ETag = entity.ETag,
                        EntityType = "recipe",
                        Title = entity.GetString("Title") ?? string.Empty,
                        Ingredients = entity.GetString("Ingredients") ?? string.Empty,
                        Steps = entity.GetString("Steps") ?? string.Empty,
                        Url = entity.GetString("Url") ?? string.Empty,
                        Category = entity.GetString("Category") ?? string.Empty
                    });
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);

            // 3. Send back a combined object so your React frontend receives both lists
            await response.WriteAsJsonAsync(new
            {
                Recipes = recipeList,
                GroceryLists = groceryLists
            });

            return response;
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();
            _logger.LogCritical(ex, "GetRecipes CRASHED. CorrelationId: {CorrelationId}", correlationId);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);

#if DEBUG
            await errorResponse.WriteStringAsync($"DEBUG ERROR: {ex.Message} \n\n {ex.StackTrace}");
#else
            await errorResponse.WriteStringAsync($"An internal error occurred. Ref: {correlationId}");
#endif
            return errorResponse;
        }
    }
}