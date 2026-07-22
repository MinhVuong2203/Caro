using Caro.Interfaces;
using Caro.Models;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace Caro.Services 
{
    public class RoomManager : IRoomManager
    {
        private readonly Dictionary<string, Room> _rooms = new();
        public Room CreateRoom(string playerName, int boardSize, string connectionId)
        {
            // Sinh mã phòng
            string roomCode = GenerateRoomCode(); 

            // Tạo người chơi mới
            Player playerCreate = new Player
            {
                ConnectionId = connectionId,
                Name = playerName,
                Symbol = 'X'
            }; 
            // Tạo phòng mới
            Room room = new Room
            {
                RoomCode = roomCode,
                Player1 = playerCreate,
                HostConnectionId = connectionId,
                BoardSize = boardSize,
                Board = new char[boardSize, boardSize],
                CurrentTurn = 'X',
                IsPlaying = false
            };

            // Lưu vào ram backend
            _rooms.Add(roomCode, room);
            return room;
        }
        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            Random random = new();

            string roomCode;

            do
            {
                roomCode = new string(
                    Enumerable.Range(0, 6)
                        .Select(_ => chars[random.Next(chars.Length)])
                        .ToArray());
            }
            while (_rooms.ContainsKey(roomCode));
            return roomCode;
        }

        public Room? JoinRoom(string roomCode, string playerName, string connectionId)
        {
            // check tồn tại phòng
            if (!_rooms.TryGetValue(roomCode, out Room? room))
            {
                throw new HubException("Phòng không tồn tại.");
            }
            // check chỗ trống
            // Vì logic ngầm là Đủ 2 player thì mới được Swap
            if (room.Player2 != null) throw new HubException("Phòng đã đầy.");

            Player playerJoin = new Player
            {
                ConnectionId = connectionId,
                Name = playerName,
                Symbol = 'O'
            };
            room.Player2 = playerJoin;
            return room;
        }

        public Room? Reconnect(string roomCode, string playerName, string connnectionId)
        {
            if (!_rooms.TryGetValue(roomCode, out var room)) return null;

            if (room.Player1?.Name == playerName)
            {
                // Cấp lại connectionId cho Player1 và cập nhật HostConnectionId nếu cần
                var oldConnectionId = room.Player1.ConnectionId;
                room.Player1.ConnectionId = connnectionId;
                if (room.HostConnectionId == oldConnectionId)
                {
                    room.HostConnectionId = connnectionId;
                }
                return room;
            }

            if (room.Player2?.Name == playerName)
            {
                var oldConnectionId = room.Player2.ConnectionId;
                room.Player2.ConnectionId = connnectionId;
                if (room.HostConnectionId == oldConnectionId)
                {
                    room.HostConnectionId = connnectionId;
                }
                return room;
            }
            return null;
        }

        public Room? LeaveRoom(string roomCode, string connectionId)
        {
            if (!_rooms.TryGetValue(roomCode, out var room))
                return null;
            // Player1 rời
            if (room.Player1?.ConnectionId == connectionId)
            {
                room.Player1 = null;
            }

            // Player2 rời
            if (room.Player2?.ConnectionId == connectionId)
            {
                room.Player2 = null;
            }

            // Không còn ai -> Xóa phòng
            if (room.Player1 == null && room.Player2 == null)
            {
                _rooms.Remove(roomCode);
                return null;
            }
            return room;
        }

    }
}
