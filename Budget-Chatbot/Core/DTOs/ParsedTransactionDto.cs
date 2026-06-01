using System.Text.Json.Serialization;

namespace BudgetChatbot.Core.DTOs;

public class ParsedTransactionDto
{
    

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("categoryId")]
    public int CategoryId { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}