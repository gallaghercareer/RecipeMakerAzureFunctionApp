using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace RecipeMakerFunctionApp;

public class UpdateRecipeCategory
{
    private readonly ILogger<UpdateRecipeCategory> _logger;

    public UpdateRecipeCategory(ILogger<UpdateRecipeCategory> logger)
    {
        _logger = logger;
    }

    [Function("UpdateRecipeCategory")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}