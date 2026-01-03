using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeMakerFunctionApp.Models
{
    public class CreateRecipeMultiResponse
    {
        //tableoutput
        [TableOutput("Recipes", Connection = "AzureWebJobsStorage")]
        public RecipeEntity? recipe { get; set; }

        [TableOutput("Recipes", Connection = "AzureWebJobsStorage")]
        public CategoryEntity? category { get; set; }
        
        [HttpResult]
        public IActionResult HttpResponse { get; set; }

        //httpresponse to client

    }
}
