namespace ManokshaApi.Services
{
    public interface IPaymentService
    {
        // create a payment order and return provider response
        Task<(bool ok, object? data)> CreatePaymentOrder(decimal amount, string currency, string receipt);
        // verify callback / signature (simplified)
        Task<bool> VerifyPaymentSignature(string payload, string signature);
    }
}
