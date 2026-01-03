using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace RecipeMakerFunctionApp;

public class DeleteRecipe
{
    private readonly ILogger<DeleteRecipe> _logger;

    public DeleteRecipe(ILogger<DeleteRecipe> logger)
    {
        _logger = logger;
    }

    [Function("DeleteRecipe")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete")] HttpRequest req)
    {
        string userId = "local-chef-123";

    #if !DEBUG
                if (req.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL-ID", out var principalIds))
                {
                    userId = principalIds.FirstOrDefault();
                }
    #endif

        
        //get recipe entity from the request
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = JsonSerializer.Deserialize<RecipeEntity>(requestBody, options);


        if (data == null || string.IsNullOrEmpty(data.RowKey))
        {
            return new BadRequestObjectResult("Recipe ID (RowKey) is required.");
        }

        try
        {
            //delete from table using rowkey (my recipe guid)
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            TableClient tableClient = new TableClient(connectionString, "Recipes");
            await tableClient.DeleteEntityAsync(userId, data.RowKey);

            _logger.LogInformation($"Deleted recipe {data.RowKey} for user {userId}");

            return new NoContentResult(); // Success (204)
        }
        catch (Exception ex)
        {
            _logger.LogError($"Delete failed: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError); //500 Failure
        }
    }
}