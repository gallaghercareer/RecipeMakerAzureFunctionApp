using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace RecipeMakerFunctionApp;

public class DeleteRecipe
{
    private readonly ILogger<DeleteRecipe> _logger;

    public DeleteRecipe(ILogger<DeleteRecipe> logger)
    {
        _logger = logger;
    }

    [Function("DeleteRecipe")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}