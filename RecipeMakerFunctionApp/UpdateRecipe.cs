using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace RecipeMakerFunctionApp;

public class UpdateRecipe
{
    private readonly ILogger<UpdateRecipe> _logger;

    public UpdateRecipe(ILogger<UpdateRecipe> logger)
    {
        _logger = logger;
    }

    [Function("UpdateRecipe")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}