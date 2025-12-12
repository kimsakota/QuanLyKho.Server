using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using QuanLyKho.Server.Services;
using System.Security.Claims;

namespace QuanLyKho.Server.Hubs
{
    [Authorize] // Yêu cầu phải có Token JWT hợp lệ mới được kết nối
    public class NotificationHub : Hub
    {
        private readonly ConnectionManager _connectionManager;

        public NotificationHub(ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        // Hàm được gọi tự động khi Client kết nối thành công
        public override Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var connectionId = Context.ConnectionId;

            if (!string.IsNullOrEmpty(userId))
            {
                _connectionManager.AddConnection(userId, connectionId);
                // (Tùy chọn) Gửi ConnectionId về lại cho client để họ biết
                Clients.Client(connectionId).SendAsync("ReceiveConnectionId", connectionId);
            }

            return base.OnConnectedAsync();
        }

        // Hàm được gọi khi Client ngắt kết nối
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var connectionId = Context.ConnectionId;

            if (!string.IsNullOrEmpty(userId))
            {
                _connectionManager.RemoveConnection(userId, connectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }

        // Ví dụ: Gửi thông báo cho một User cụ thể
        public async Task SendToUser(string targetUserId, string message)
        {
            var connections = _connectionManager.GetConnections(targetUserId);
            if (connections != null && connections.Any())
            {
                // Gửi đến tất cả các thiết bị (connection) của user đó
                foreach (var connectionId in connections)
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveNotification", message);
                }
            }
        }
    }
}