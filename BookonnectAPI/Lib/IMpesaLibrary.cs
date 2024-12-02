using BookonnectAPI.Models;

namespace BookonnectAPI.Lib;

// TODO: Implement when LIPA NA MPESA till is ready
public interface IMpesaLibrary
{
    public Task<MpesaAuthToken?> GetMpesaAuthToken();
    public Task<TransactionStatusResponse?> GetTransactionStatusResponse(string transactionID, string accessToken, int orderID);
    public Task<TransactionStatusResponse?> MakeBusinessPayment(float amount, long recipientPhoneNumber, string accessToken, int orderID);
}

