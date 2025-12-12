using System.Collections.Concurrent;

namespace QuanLyKho.Server.Services
{
    public class ConnectionManager
    {
        // Dùng ConcurrentDictionary để đảm bảo an toàn luồng (thread-safe)
        // Key: UserId (string), Value: List các ConnectionId (một user có thể đăng nhập nhiều nơi)
        private static readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();

        public void AddConnection(string userId, string connectionId)
        {
            _userConnections.AddOrUpdate(userId,
                new HashSet<string> { connectionId },
                (key, existingConnections) =>
                {
                    lock (existingConnections)
                    {
                        existingConnections.Add(connectionId);
                    }
                    return existingConnections;
                });
        }

        public void RemoveConnection(string userId, string connectionId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(connectionId);
                    if (connections.Count == 0)
                    {
                        _userConnections.TryRemove(userId, out _);
                    }
                }
            }
        }

        public IEnumerable<string> GetConnections(string userId)
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                return connections;
            }
            return Enumerable.Empty<string>();
        }

        // Lấy danh sách tất cả user đang online (tùy chọn)
        public IEnumerable<string> GetOnlineUsers()
        {
            return _userConnections.Keys;
        }
    }
}