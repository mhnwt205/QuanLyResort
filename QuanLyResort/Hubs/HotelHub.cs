using Microsoft.AspNetCore.SignalR;

namespace QuanLyResort.Hubs
{
    public class HotelHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task NotifyBookingUpdated(int bookingId)
        {
            await Clients.All.SendAsync("BookingUpdated", bookingId);
        }

        public async Task NotifyRoomStatusChanged(int roomId, string status)
        {
            await Clients.All.SendAsync("RoomStatusChanged", roomId, status);
        }

        public async Task NotifyNewBooking(int bookingId, string bookingCode)
        {
            await Clients.All.SendAsync("NewBooking", bookingId, bookingCode);
        }

        public async Task NotifyCheckIn(int bookingId, string roomNumber)
        {
            await Clients.All.SendAsync("CheckIn", bookingId, roomNumber);
        }

        public async Task NotifyCheckOut(int bookingId, string roomNumber)
        {
            await Clients.All.SendAsync("CheckOut", bookingId, roomNumber);
        }

        public async Task NotifyPaymentProcessed(int paymentId, string paymentNumber, decimal amount)
        {
            await Clients.All.SendAsync("PaymentProcessed", paymentId, paymentNumber, amount);
        }

        public async Task NotifyPaymentRefunded(int paymentId, string paymentNumber, decimal refundAmount)
        {
            await Clients.All.SendAsync("PaymentRefunded", paymentId, paymentNumber, refundAmount);
        }

        public async Task NotifyLowStockAlert(int itemId, string itemName, int quantity)
        {
            await Clients.All.SendAsync("LowStockAlert", itemId, itemName, quantity);
        }

        public async Task NotifyInvoiceGenerated(int invoiceId, string invoiceNumber)
        {
            await Clients.All.SendAsync("InvoiceGenerated", invoiceId, invoiceNumber);
        }

        public async Task NotifyInvoiceUpdated(int invoiceId, string invoiceNumber, string status)
        {
            await Clients.All.SendAsync("InvoiceUpdated", invoiceId, invoiceNumber, status);
        }

        public async Task NotifyInvoiceApproved(int invoiceId, string invoiceNumber)
        {
            await Clients.All.SendAsync("InvoiceApproved", invoiceId, invoiceNumber);
        }

        public async Task NotifyInvoiceCancelled(int invoiceId, string invoiceNumber)
        {
            await Clients.All.SendAsync("InvoiceCancelled", invoiceId, invoiceNumber);
        }

        public async Task NotifyServiceBookingCreated(int serviceBookingId, string serviceName)
        {
            await Clients.All.SendAsync("ServiceBookingCreated", serviceBookingId, serviceName);
        }

        public async Task NotifyStockIssueCreated(int issueId, string issueNumber)
        {
            await Clients.All.SendAsync("StockIssueCreated", issueId, issueNumber);
        }

        public async Task NotifyDashboardUpdate()
        {
            await Clients.All.SendAsync("DashboardUpdate");
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            // Join admin group by default
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admin");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
