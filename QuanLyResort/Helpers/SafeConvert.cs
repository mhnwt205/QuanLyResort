using QuanLyResort.Models.Enums;

namespace QuanLyResort.Helpers
{
    public static class SafeConvert
    {
        public static int ToInt(this int? value, int defaultValue = 0) => value ?? defaultValue;
        
        public static DateTime ToDate(this DateTime? value, DateTime? defaultValue = null) => value ?? defaultValue ?? DateTime.UtcNow;
        
        public static decimal ToDecimal(this decimal? value, decimal defaultValue = 0) => value ?? defaultValue;
        
        public static string ToString(this string? value, string defaultValue = "") => value ?? defaultValue;
        
        public static bool ToBool(this bool? value, bool defaultValue = false) => value ?? defaultValue;
        
        public static DateOnly ToDateOnly(this DateOnly? value, DateOnly? defaultValue = null) => value ?? defaultValue ?? DateOnly.FromDateTime(DateTime.UtcNow);
        
        public static RoomStatus ToRoomStatus(this string? value, RoomStatus defaultValue = RoomStatus.Available)
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;
            
            return value.ToLower() switch
            {
                "available" => RoomStatus.Available,
                "occupied" => RoomStatus.Occupied,
                "outoforder" or "out_of_order" => RoomStatus.OutOfOrder,
                "cleaning" => RoomStatus.Cleaning,
                "maintenance" => RoomStatus.Maintenance,
                _ => defaultValue
            };
        }
        
        public static string ToRoomStatusString(this RoomStatus status)
        {
            return status switch
            {
                RoomStatus.Available => "available",
                RoomStatus.Occupied => "occupied",
                RoomStatus.OutOfOrder => "out_of_order",
                RoomStatus.Cleaning => "cleaning",
                RoomStatus.Maintenance => "maintenance",
                _ => "available"
            };
        }
    }
}
