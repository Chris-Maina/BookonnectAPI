using BookonnectAPI.Models;

namespace BookonnectAPI.Lib;

public interface IMpesaLibrary
{
    public Task<MpesaAuthToken?> GetMpesaAuthToken();
    public Task<TransactionStatusResponse?> GetTransactionStatusResponse(PaymentDTO paymentDTO, string accessToken);
}

