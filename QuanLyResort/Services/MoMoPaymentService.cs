using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using QuanLyResort.Models;

namespace QuanLyResort.Services
{
    public interface IMoMoPaymentService
    {
        Task<MoMoPaymentResponse> CreatePaymentAsync(MoMoPaymentRequest request);
        bool VerifySignature(MoMoCallbackData callbackData);
        string GenerateSignature(string rawHash);
    }

    public class MoMoPaymentService : IMoMoPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _endpoint = "https://test-payment.momo.vn/v2/gateway/api/create";

        public MoMoPaymentService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<MoMoPaymentResponse> CreatePaymentAsync(MoMoPaymentRequest request)
        {
            try
            {
                // Tạo signature - sử dụng thứ tự đúng theo MoMo API
                var rawHash = $"accessKey={request.AccessKey}&amount={request.Amount}&extraData={request.ExtraData}&ipnUrl={request.NotifyUrl}&orderId={request.OrderId}&orderInfo={request.OrderInfo}&partnerCode={request.PartnerCode}&redirectUrl={request.ReturnUrl}&requestId={request.RequestId}&requestType={request.RequestType}";
                request.Signature = GenerateSignature(rawHash);

                // Validate request trước khi serialize
                if (string.IsNullOrEmpty(request.PartnerCode) || string.IsNullOrEmpty(request.AccessKey) || 
                    string.IsNullOrEmpty(request.OrderId) || request.Amount <= 0)
                {
                    return new MoMoPaymentResponse
                    {
                        ResultCode = -1,
                        Message = "Request validation failed: Missing required fields"
                    };
                }

                // Serialize request
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Gửi request đến MoMo
                Console.WriteLine($"→ Sending request to MoMo: {_endpoint}");
                Console.WriteLine($"→ Request JSON: {json}");
                
                var response = await _httpClient.PostAsync(_endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"→ MoMo Response Status: {response.StatusCode}");
                Console.WriteLine($"→ MoMo Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<MoMoPaymentResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    return result ?? new MoMoPaymentResponse { ResultCode = -1, Message = "Không thể parse response" };
                }
                else
                {
                    return new MoMoPaymentResponse
                    {
                        ResultCode = -1,
                        Message = $"HTTP Error: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new MoMoPaymentResponse
                {
                    ResultCode = -1,
                    Message = $"Lỗi: {ex.Message}"
                };
            }
        }

        public bool VerifySignature(MoMoCallbackData callbackData)
        {
            try
            {
                var rawHash = $"accessKey={callbackData.AccessKey}&amount={callbackData.Amount}&extraData={callbackData.ExtraData}&message={callbackData.Message}&orderId={callbackData.OrderId}&orderInfo={callbackData.OrderInfo}&orderType={callbackData.OrderType}&partnerCode={callbackData.PartnerCode}&payType={callbackData.PayType}&requestId={callbackData.RequestId}&responseTime={callbackData.ResponseTime}&resultCode={callbackData.ResultCode}&transId={callbackData.TransId}";
                var expectedSignature = GenerateSignature(rawHash);
                return expectedSignature == callbackData.Signature;
            }
            catch
            {
                return false;
            }
        }

        public string GenerateSignature(string rawHash)
        {
            var secretKey = _configuration["MoMo:SecretKey"] ?? "K951B6PE1waDMi640xX08PD3vg6EkVlz";
            using var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hashBytes = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(rawHash));
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
}
