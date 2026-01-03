using Azure.Data.Tables;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using RecipeMakerFunctionApp.Models;

namespace RecipeMakerFunctionApp;

public class UpdateRecipe
{
    private readonly ILogger<UpdateRecipe> _logger;

    public UpdateRecipe(ILogger<UpdateRecipe> logger)
    {
        _logger = logger;
    }

    [Function("UpdateRecipe")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put")] HttpRequest req)
    {
        string userId = "local-chef-123";
#if !DEBUG
        if (req.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL-ID", out var principalIds))
        {
            userId = principalIds.FirstOrDefault();
        }
#endif

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var updatedData = JsonSerializer.Deserialize<RecipeEntity>(requestBody, options);

        if (updatedData == null || string.IsNullOrEmpty(updatedData.RowKey))
        {
            return new BadRequestObjectResult("RowKey is required to update a recipe.");
        }

        try
        {
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            TableClient tableClient = new TableClient(connectionString, "Recipes");

            updatedData.PartitionKey = userId;
            updatedData.ETag = ETag.All;

            // 1. Update the Recipe itself
            await tableClient.UpdateEntityAsync(updatedData, updatedData.ETag, TableUpdateMode.Replace);

            // 2. NEW: Ensure the new category exists (Upsert)
            if (!string.IsNullOrEmpty(updatedData.Category))
            {
                string safeCategoryKey = updatedData.Category.Replace(" ", "_").ToLower();

                CategoryEntity NewCategory = new CategoryEntity()
                {
                    PartitionKey = userId,
                    RowKey = "category_" + safeCategoryKey,
                    CategoryName = updatedData.Category,
                    EntityType = "Category"
                };

                // This ensures the new category folder is created/updated in the table
                await tableClient.UpsertEntityAsync(NewCategory, TableUpdateMode.Replace);
            }

            _logger.LogInformation($"Updated recipe {updatedData.RowKey} and ensured category marker for user {userId}");

            return new OkObjectResult(updatedData);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return new NotFoundObjectResult("Recipe not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Update failed: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}