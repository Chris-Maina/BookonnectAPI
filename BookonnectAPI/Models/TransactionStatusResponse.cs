namespace BookonnectAPI.Models;

// Mpesa Transaction Status
public class TransactionStatusResponse
{
    public string OriginatorConversationID { get; } = String.Empty;
    public string ConversationID { get; } = String.Empty;
    public int ResponseCode { get; }
    public string ResponseDescription { get; } = String.Empty;
}

