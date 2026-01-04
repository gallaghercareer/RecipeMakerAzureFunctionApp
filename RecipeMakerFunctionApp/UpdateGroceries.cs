using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RecipeMakerFunctionApp.Models;
using System.Linq;
using System.Text.Json;

namespace RecipeMakerFunctionApp;

public class UpdateGroceries
{
    private readonly ILogger<UpdateGroceries> _logger;

    public UpdateGroceries(ILogger<UpdateGroceries> logger)
    {
        _logger = logger;
    }

    [Function("UpdateGroceries")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "put")] HttpRequest req)
    {
        string userId = "local-chef-123";

        _logger.LogInformation("Attempting to update groceries for user: {UserId}", userId);

        try
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<GroceriesEntity>(requestBody, options);

            if (data == null)
            {
                _logger.LogWarning("UpdateGroceries received null data for user: {UserId}", userId);
                return new BadRequestObjectResult("Invalid data.");
            }

            var groceryEntity = new GroceriesEntity
            {
                PartitionKey = userId,
                Items = JsonSerializer.Serialize(data.Items)
            };

            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            TableClient tableClient = new TableClient(connectionString, "Recipes");

            await tableClient.UpsertEntityAsync(groceryEntity, TableUpdateMode.Replace);

            // SUCCESS LOG: Confirms data hit the database
            int dataSize = data.Items?.Length ?? 0;
            _logger.LogInformation("Successfully updated groceries. Data size: {Size} characters for user {UserId}", dataSize, userId);
            return new OkObjectResult(data.Items);
        }
        catch (Exception ex)
        {
            // ERROR LOG: Passing 'ex' as the first argument captures the Stack Trace
            _logger.LogError(ex, "Critical failure updating groceries for user {UserId}", userId);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}
