using Microsoft.EntityFrameworkCore;
using QuanLyResort.Models;

namespace QuanLyResort
{
    public static class DataSeeder
    {
        public static async Task SeedDataAsync(ResortDbContext context)
        {
            // Kiểm tra xem đã có dữ liệu chưa
            if (await context.RoomTypes.AnyAsync())
            {
                return; // Đã có dữ liệu, không cần seed
            }

            // Thêm RoomTypes
            var roomTypes = new List<RoomType>
            {
                new RoomType
                {
                    TypeName = "Suite",
                    Name = "Phòng Suite",
                    Description = "Phòng suite sang trọng với view biển tuyệt đẹp",
                    BasePrice = 3000000,
                    MaxOccupancy = 3,
                    Capacity = 3,
                    Amenities = "WiFi, TV, Mini bar, View biển, Bồn tắm jacuzzi"
                },
                new RoomType
                {
                    TypeName = "Family",
                    Name = "Phòng Gia Đình",
                    Description = "Phòng rộng rãi phù hợp cho gia đình",
                    BasePrice = 2500000,
                    MaxOccupancy = 4,
                    Capacity = 4,
                    Amenities = "WiFi, TV, 2 giường đôi, View biển, Bếp mini"
                },
                new RoomType
                {
                    TypeName = "Deluxe",
                    Name = "Phòng Deluxe",
                    Description = "Phòng deluxe cao cấp với tiện nghi hiện đại",
                    BasePrice = 4000000,
                    MaxOccupancy = 5,
                    Capacity = 5,
                    Amenities = "WiFi, Smart TV, Mini bar, View biển, Bồn tắm jacuzzi, Balcony"
                },
                new RoomType
                {
                    TypeName = "Classic",
                    Name = "Phòng Classic",
                    Description = "Phòng classic với thiết kế truyền thống",
                    BasePrice = 3500000,
                    MaxOccupancy = 4,
                    Capacity = 4,
                    Amenities = "WiFi, TV, 2 giường đôi, View biển"
                },
                new RoomType
                {
                    TypeName = "Superior",
                    Name = "Phòng Superior",
                    Description = "Phòng superior với view đẹp",
                    BasePrice = 2800000,
                    MaxOccupancy = 3,
                    Capacity = 3,
                    Amenities = "WiFi, TV, View biển, Balcony"
                },
                new RoomType
                {
                    TypeName = "Luxury",
                    Name = "Phòng Luxury",
                    Description = "Phòng luxury cao cấp nhất",
                    BasePrice = 5000000,
                    MaxOccupancy = 6,
                    Capacity = 6,
                    Amenities = "WiFi, Smart TV, Mini bar, View biển, Bồn tắm jacuzzi, Balcony, Butler service"
                }
            };

            context.RoomTypes.AddRange(roomTypes);
            await context.SaveChangesAsync();

            // Thêm Rooms
            var rooms = new List<Room>();
            var random = new Random();

            for (int i = 1; i <= 20; i++)
            {
                var roomTypeId = (i % 6) + 1; // Phân bố đều các loại phòng
                var roomType = roomTypes.First(rt => rt.RoomTypeId == roomTypeId);
                
                rooms.Add(new Room
                {
                    RoomNumber = $"R{i:D3}",
                    RoomTypeId = roomTypeId,
                    FloorNumber = (i / 4) + 1, // 4 phòng mỗi tầng
                    Status = "available",
                    Price = roomType.BasePrice + random.Next(-200000, 500000), // Giá dao động
                    MaxOccupancy = roomType.MaxOccupancy,
                    Description = $"{roomType.Description} - Phòng {i}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            context.Rooms.AddRange(rooms);
            await context.SaveChangesAsync();

            Console.WriteLine("Đã thêm dữ liệu mẫu thành công!");
        }
    }
}
