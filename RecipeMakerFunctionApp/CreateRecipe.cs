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


#if DEBUG
        //local testing
        string userId = "local-chef-123"; 
#else      
        //production
        if (req.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL-ID", out var principalIds))
        {
            userId = principalIds.FirstOrDefault();
        }
#endif
       
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonSerializer.Deserialize<RecipeEntity>(requestBody);

        _logger.LogInformation($"Creating recipe for user: {userId}");

        
       RecipeEntity NewRecipe = new RecipeEntity()
        {
            PartitionKey = userId,
            RowKey = Guid.NewGuid().ToString(),
            Title = data.Title,
            Ingredients = data.Ingredients,
            Steps = data.Steps,
            Url = data.Url,
            Category = data.Category
        };

        return new CreateRecipeMultiResponse
        {
            // This property sends the data to the queue
            recipe = NewRecipe,

            // This property sends the response to the website
            HttpResponse = new OkObjectResult(NewRecipe)
        };
    }
}