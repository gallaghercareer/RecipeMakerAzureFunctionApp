using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RecipeMakerFunctionApp.Models;
using System.Text.Json;

namespace RecipeMakerFunctionApp;

public class CreateRecipe
{
    private readonly ILogger<CreateRecipe> _logger;

    public CreateRecipe(ILogger<CreateRecipe> logger)
    {
        _logger = logger;
    }


    [Function("CreateRecipe")]
    public async Task<CreateRecipeMultiResponse> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
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

        var data = JsonSerializer.Deserialize<RecipeEntity>(requestBody, options);
        
        //if (data == null || string.IsNullOrEmpty(data.Category))
        //{
        //    _logger.LogError("Invalid recipe data received.");

        //    // We must return a valid response object that indicates failure
        //    // We'll use a 400 Bad Request
        //    return new CreateRecipeMultiResponse
        //    {
        //        HttpResponse = new BadRequestObjectResult("Recipe data or Category is missing.")
        //    };
        //}
        _logger.LogInformation($"Creating recipe for user: {userId}");

        
       RecipeEntity NewRecipe = new RecipeEntity()
        {
            PartitionKey = userId,
            RowKey = "recipe_" + Guid.NewGuid().ToString(),
            Title = data.Title,
            Ingredients = data.Ingredients,
            Steps = data.Steps,
            Url = data.Url,
            Category = data.Category    
        };

        // Sanitizing the RowKey for the database
        string safeCategoryKey = data.Category.Replace(" ", "_").ToLower();

        CategoryEntity NewCategory = new CategoryEntity()
        {
            PartitionKey = userId,
            RowKey = "category_" + safeCategoryKey,
            CategoryName = data.Category// data.Category

        };

        //if category already exists in a row, upsert
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        TableClient tableClient = new TableClient(connectionString, "Recipes");
        await tableClient.UpsertEntityAsync(NewCategory, TableUpdateMode.Replace);

        _logger.LogInformation($"DEBUG: NewCategory.CategoryName is: {NewCategory.CategoryName}");
        return new CreateRecipeMultiResponse
        {
            // This  creates the recipe row
            Recipe = NewRecipe,

            //null due to upsert change
            Category = null,

            // This property sends the response to the website
            HttpResponse = new OkObjectResult(new
            {
                recipe = NewRecipe,
                category = NewCategory
            })
            

        };
    }
}