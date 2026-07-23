using Caro.DTOs;
using Caro.Interfaces;
using Caro.Mapper;
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
        public async Task<RoomResponse> CreateRoom(CreateRoomRequest request)
        {
            var room = _roomManager.CreateRoom(request.PlayerName, request.BoardSize, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);
            return RoomMapper.ToResponse(room);
        }
        // Join Room
        public async Task<RoomResponse> JoinRoom(JoinRoomRequest request)
        {
            var room = _roomManager.JoinRoom(request.RoomCode, request.PlayerName, Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);

            var response = RoomMapper.ToResponse(room);

            await Clients.Group(room.RoomCode)
                .SendAsync("RoomUpdated", response);

            return response;
        }

        // Reconnect room (when F5)
        // Tạm thời trả về JoinRoomResponse
        public async Task<RoomResponse> Reconnect(ReconnectRequest request)
        {
            var room = _roomManager.Reconnect(request.RoomCode, request.PlayerName, Context.ConnectionId);

            if (room == null) throw new HubException("Không tìm thấy cuộc chơi!");

            await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);

            return RoomMapper.ToResponse(room);
        }

        // Rời phòng
        public async Task LeaveRoom(string roomCode)
        {
            var room = _roomManager.LeaveRoom(roomCode, Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);

            // nếu phòng vẫn còn người thì cập nhật cho người còn lại
            if (room != null)
            {
                await Clients.Group(roomCode).SendAsync("RoomUpdated", RoomMapper.ToResponse(room));
            }
        }

        // Swap
        public async Task<RoomResponse> SwapPlayer(SwapPlayerRequest request)
        {
            var room = _roomManager.SwapPlayer(
                request.RoomCode,
                Context.ConnectionId,          // Người yêu cầu (Host)
                request.SourceConnectionId,
                request.TargetConnectionId);

            var response = RoomMapper.ToResponse(room);

            await Clients.Group(room.RoomCode)
                .SendAsync("RoomUpdated", response);

            return response;
        }

        // Bắt đầu
        public async Task StartGame(string roomCode)
        {
            var room = _roomManager.StartGame(roomCode, Context.ConnectionId);

            await Clients.Group(room.RoomCode)
                .SendAsync("RoomUpdated", RoomMapper.ToResponse(room));
        }

        public async Task StopGame(string roomCode)
        {
            var room = _roomManager.StopGame(roomCode, Context.ConnectionId);
            await Clients.Group(room.RoomCode)
                .SendAsync("RoomUpdated", RoomMapper.ToResponse(room));
        }

        public async Task RestartGame(string roomCode)
        {
            var room = _roomManager.RestartGame(roomCode, Context.ConnectionId);
            await Clients.Group(room.RoomCode)
                .SendAsync("RoomUpdated", RoomMapper.ToResponse(room));
        }

        public async Task PlacePiece(PlacePieceRequest request)
        {
            var room = _roomManager.PlacePiece(request.RoomCode, Context.ConnectionId, request.Row, request.Col);
            if (room == null) return;
            await Clients.Group(room.RoomCode).SendAsync("RoomUpdated", RoomMapper.ToResponse(room));
        }

    }
}

// Ghi chú
// Thay vì
//return new RoomResponse
//{
//    RoomCode = room.RoomCode,
//    BoardSize = room.BoardSize,
//    CurrentTurn = room.CurrentTurn,
//    IsPlaying = room.IsPlaying,
//    Player1 = room.Player1,
//    Player2 = room.Player2,
//    HostConnectionId = room.HostConnectionId
//    Board = ??? do char[][] khác char[,]
//};

//Nên phải viết hàm Mapper riêng và tái sử dụng
//RoomMapper.ToResponse(room) : dữ liệu trả về RoomResponse