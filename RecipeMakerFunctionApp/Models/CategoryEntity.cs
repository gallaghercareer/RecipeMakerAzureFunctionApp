using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeMakerFunctionApp.Models
{
    public class CategoryEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = default!; // This will be your UserID (oid)
        public string RowKey { get; set; } = default!;       // This will be a unique Recipe ID (Guid)
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string EntityType { get; set; } = "Category";
        public string? CategoryName { get; set; }
    }
}
