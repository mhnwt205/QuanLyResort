using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuanLyResort.Models
{
    public class MoMoPaymentRequest
    {
        [Required]
        public string PartnerCode { get; set; } = "MOMO";
        
        [Required]
        public string AccessKey { get; set; } = "F8BBA842ECF85";
        
        [Required]
        public string SecretKey { get; set; } = "K951B6PE1waDMi640xX08PD3vg6EkVlz";
        
        [Required]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public long Amount { get; set; }
        
        [Required]
        public string OrderId { get; set; } = "";
        
        [Required]
        public string OrderInfo { get; set; } = "";
        
        [Required]
        [JsonPropertyName("redirectUrl")]
        public string ReturnUrl { get; set; } = "";
        
        [Required]
        [JsonPropertyName("ipnUrl")]
        public string NotifyUrl { get; set; } = "";
        
        [Required]
        public string RequestType { get; set; } = "captureWallet";
        
        [Required]
        public string Signature { get; set; } = "";
        
        public string ExtraData { get; set; } = "";
    }

    public class MoMoPaymentResponse
    {
        public int ResultCode { get; set; }
        public string Message { get; set; } = "";
        public string PayUrl { get; set; } = "";
        public string Deeplink { get; set; } = "";
        public string QrCodeUrl { get; set; } = "";
    }

    public class MoMoCallbackData
    {
        public string PartnerCode { get; set; } = "";
        public string AccessKey { get; set; } = "";
        public string RequestId { get; set; } = "";
        public long Amount { get; set; }
        public string OrderId { get; set; } = "";
        public string OrderInfo { get; set; } = "";
        public string OrderType { get; set; } = "";
        public long TransId { get; set; }
        public int ResultCode { get; set; }
        public string Message { get; set; } = "";
        public string PayType { get; set; } = "";
        public long ResponseTime { get; set; }
        public string ExtraData { get; set; } = "";
        public string Signature { get; set; } = "";
    }
}
