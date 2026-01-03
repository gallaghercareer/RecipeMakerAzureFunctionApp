using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeMakerFunctionApp.Models
{
    public class CreateCategoryMultiResponse
    {
       
            //tableoutput
            [TableOutput("Recipes", Connection = "AzureWebJobsStorage")]
       
            public CategoryEntity? Category { get; set; }

            //httpresponse to client

            [HttpResult]
            public IActionResult? HttpResponse { get; set; }


       
    }
}
