using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RecipeMakerFunctionApp.Models;
using System.Text.Json;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace RecipeMakerFunctionApp;

public class CreateRecipeCategory
{
    private readonly ILogger<CreateRecipeCategory> _logger;

    public CreateRecipeCategory(ILogger<CreateRecipeCategory> logger)
    {
        _logger = logger;
    }

    [Function("CreateRecipeCategory")]
    public async Task<CreateCategoryMultiResponse> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        string userId = "local-chef-123";

#if !DEBUG

        //production
        if (req.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL-ID", out var principalIds))
        {
            userId = principalIds.FirstOrDefault();
        }
#endif
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();


        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var data = JsonSerializer.Deserialize<CategoryEntity>(requestBody, options);

        _logger.LogInformation($"Creating category for user: {userId}");

        // Sanitizing the RowKey for the database
        string safeCategoryKey = data.CategoryName.Replace(" ", "_").ToLower();

        CategoryEntity NewCategory = new CategoryEntity()
        {
            PartitionKey = userId,
            RowKey = "category_" + safeCategoryKey,
            CategoryName = data.CategoryName// data.Category

        };

        //if category already exists in a row, upsert
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        TableClient tableClient = new TableClient(connectionString, "Recipes");
        await tableClient.UpsertEntityAsync(NewCategory, TableUpdateMode.Replace);

        _logger.LogInformation($"DEBUG: NewCategory.CategoryName is: {NewCategory.CategoryName}");

        return new CreateCategoryMultiResponse
        {

            Category = null,

            // This property sends the response to the website
            HttpResponse = new OkObjectResult(new
            {
                      
                category = NewCategory
            })
        };
    }
}