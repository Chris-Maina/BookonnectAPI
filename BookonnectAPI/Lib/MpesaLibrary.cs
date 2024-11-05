using System.Net.Http.Headers;
using System.Text;
using BookonnectAPI.Models;
using Newtonsoft.Json;

namespace BookonnectAPI.Lib;

public class MpesaLibrary: IMpesaLibrary
{
    const string consumerKey = "Authentication:Mpesa:ConsumerKey";
    const string consumerSectret = "Authentication:Mpesa:ConsumerSecret";
    const string securityCredentialKey = "Mpesa:SecurityCredential";
    const string initiatorNameKey = "Mpesa:InitiatorName";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MpesaLibrary> _logger;
    private readonly IConfiguration _configuration;


    public MpesaLibrary(IHttpClientFactory httpClientFactory, ILogger<MpesaLibrary> logger, IConfiguration configuration)
	{
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    private async Task<string> GetAuthorizationToken()
    {

        var httpClient = _httpClientFactory.CreateClient("Safaricom");
        var authenticationString = $"{_configuration[consumerKey]}:{_configuration[consumerSectret]}";
        var base64String = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));

        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, "/oauth/v1/generate?grant_type=client_credentials");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64String);

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
        catch (OperationCanceledException ex) when (ex.InnerException is TimeoutException tex)
        {
            _logger.LogError(ex, $"Timed out: {ex.Message}, {tex.Message}");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"Request failed with status code: {ex.StatusCode}");
            throw;
        }
    }

    private async Task<string> PostTransactionStatus(string transactionID, string accessToken)
    {

        // construct mpesa request and send it
        var httpClient = _httpClientFactory.CreateClient("Safaricom");
        var payload = new Dictionary<string, string>
        {
            { "Initiator", _configuration[initiatorNameKey]! },
            { "SecurityCredential" , "UQcIopoI8SXUjT5f0Su8TzIC6F2XH9NM3Otqm6sYItOHeaP6cbz0pQgWNlLZCCpdnH9KV8enUqsV5bBBre" },
            { "CommandID" , "TransactionStatusQuery" },
            { "TransactionID", $"{transactionID}" },
            // PartyA = 7845640,
            { "PartyA" , "600984" },
            { "IdentifierType" , "4" },
            { "ResultURL" , "https://mydomain.com/TransactionStatus/result/" }, // must be a https domain. modify after hosting
            { "QueueTimeOutURL" , "https://mydomain.com/TransactionStatus/queue/" },
            { "Remarks" ,"OK" },
        };
        string jsonPayloadString = JsonConvert.SerializeObject(payload);

        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, "/mpesa/transactionstatus/v1/query");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        requestMessage.Content = new StringContent(jsonPayloadString, Encoding.UTF8, "application/json");

        try
        {
            using var response = await httpClient.SendAsync(requestMessage);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (OperationCanceledException ex) when (ex.InnerException is TimeoutException tex)
        {
            _logger.LogError(ex, $"Timed out: {ex.Message}, {tex.Message}");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"Request failed with status code: {ex.StatusCode}");
            throw;
        }

    }

    private async Task<string> PostBusinessPayment(string amount, string recipientPhoneNumber, string accessToken, int orderID)
    {
        // construct mpesa request and send it
        var httpClient = _httpClientFactory.CreateClient("Safaricom");
        var payload = new Dictionary<string, string>
        {
            { "InitiatorName", _configuration[initiatorNameKey]! },
            { "SecurityCredential", _configuration[securityCredentialKey]! },
            { "CommandID", "BusinessPayment" },
            { "Amount" , amount },
             // PartyA = 7845640,
            { "PartyA" , "600984" },
            { "PartyB", recipientPhoneNumber },
            { "Remarks" ,"OK" },
            { "QueueTimeOutURL" , "https://mydomain.com/b2c/queue/" },
            { "ResultURL" , $"https://mydomain.com/b2c/result/?orderID={orderID}" },
            { "Occassion" , "Book payment" },
        };
        string jsonPayloadString = JsonConvert.SerializeObject(payload);
        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, "/mpesa/b2c/v3/paymentrequest");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        requestMessage.Content = new StringContent(jsonPayloadString, Encoding.UTF8, "application/json");

        try
        {
            using var response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (OperationCanceledException ex) when (ex.InnerException is TimeoutException tex)
        {
            _logger.LogError(ex, $"Timed out: {ex.Message}, {tex.Message}");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"Request failed with status code: {ex.StatusCode}");
            throw;
        }
    }

    public async Task<TransactionStatusResponse?> GetTransactionStatusResponse(string transactionID, string accessToken)
    {
        try
        {
            string response = await PostTransactionStatus(transactionID, accessToken);
            if(string.IsNullOrEmpty(response))
            {
                return null;
            }
            return JsonConvert.DeserializeObject<TransactionStatusResponse>(response);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<MpesaAuthToken?> GetMpesaAuthToken()
    {
        string response = await GetAuthorizationToken();
        if (string.IsNullOrEmpty(response))
        {
            return null;
        }
        return JsonConvert.DeserializeObject<MpesaAuthToken>(response);
    }

    public async Task<TransactionStatusResponse?> MakeBusinessPayment(string amount, string recipientPhoneNumber, string accessToken, int orderID)
    {
        try
        {
            string response = await PostBusinessPayment(amount, recipientPhoneNumber, accessToken, orderID);
            if (string.IsNullOrEmpty(response))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<TransactionStatusResponse>(response);
        }
        catch (Exception)
        {
            throw;
        }
    }


}

