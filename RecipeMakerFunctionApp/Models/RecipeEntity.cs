using Azure;
using Azure.Data.Tables;

public class RecipeEntity : ITableEntity
{
    // Required by Table Storage
    public string PartitionKey { get; set; } = default!; // This will be your UserID (oid)
    public string RowKey { get; set; } = default!;       // This will be a unique Recipe ID (Guid)
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    //  Custom Recipe Fields
    public string Title { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Steps { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string EntityType { get; set; } = "recipe";

}