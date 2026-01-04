using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeMakerFunctionApp.Models
{
   public class GroceriesEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = default!; // This will be your UserID (oid)
        public string RowKey { get; set; } = "GROCERY_LIST";       // This will be a unique Recipe ID (Guid)
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string EntityType { get; set; } = "GroceryList";
        
        //we don't use List<string> b/c azure doesn't know what a list<string> is
        public string? Items { get; set; } = "[]";
    }
}
