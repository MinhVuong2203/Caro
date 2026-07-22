using Caro.DTOs;
using Caro.Interfaces;
using Caro.Models;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Caro.Hubs
{
    public class GameHub : Hub
    {
        private readonly IRoomManager _roomManager;
        public GameHub(IRoomManager roomManager)
        {
            _roomManager = roomManager;
        }
        // Create room
        public async Task<CreateRoomResponse> CreateRoom(CreateRoomRequest request)
        {
            var room = _roomManager.CreateRoom(request.PlayerName, request.BoardSize, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);
            return new CreateRoomResponse
            {
                RoomCode = room.RoomCode,
                BoardSize = room.BoardSize,
                CurrentTurn = room.CurrentTurn,
                IsPlaying = room.IsPlaying,
                Player1 = room.Player1,  
                HostConnectionId = room.HostConnectionId
            };
        }
        // Join Room
        public async Task<JoinRoomResponse> JoinRoom(JoinRoomRequest request)
        {
            var room = _roomManager.JoinRoom(request.RoomCode, request.PlayerName, Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);

            var response = new JoinRoomResponse
            {
                RoomCode = room.RoomCode,
                BoardSize = room.BoardSize,
                CurrentTurn = room.CurrentTurn,
                IsPlaying = room.IsPlaying,
                Player1 = room.Player1,
                Player2 = room.Player2,
                HostConnectionId = room.HostConnectionId
            };

            await Clients.Group(room.RoomCode)
                .SendAsync("RoomUpdated", response);

            return response;
        }
        // Reconnect room (when F5)
        // Tạm thời trả về JoinRoomResponse
        public async Task<JoinRoomResponse> Reconnect(ReconnectRequest request)
        {
            var room = _roomManager.Reconnect(request.RoomCode, request.PlayerName, Context.ConnectionId);

            if (room == null) throw new HubException("Không tìm thấy cuộc chơi!");

            await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);

            return new JoinRoomResponse
            {
                RoomCode = room.RoomCode,
                BoardSize = room.BoardSize,
                CurrentTurn = room.CurrentTurn,
                IsPlaying = room.IsPlaying,
                Player1 = room.Player1,
                Player2 = room.Player2,
                HostConnectionId = room.HostConnectionId
            };
        }

        // Rời phòng
        public async Task LeaveRoom(string roomCode)
        {
            var room = _roomManager.LeaveRoom(roomCode, Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);

            // nếu phòng vẫn còn người thì cập nhật cho người còn lại
            if (room != null)
            {
                await Clients.Group(roomCode).SendAsync("RoomUpdated", new JoinRoomResponse
                {
                    RoomCode = room.RoomCode,
                    BoardSize = room.BoardSize,
                    CurrentTurn = room.CurrentTurn,
                    IsPlaying = room.IsPlaying,
                    Player1 = room.Player1,
                    Player2 = room.Player2,
                    HostConnectionId = room.HostConnectionId
                });
            }
        }
    }
}
