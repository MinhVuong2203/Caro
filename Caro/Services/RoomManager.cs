using Caro.Interfaces;
using Caro.Models;
using Caro.Utils;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Caro.Services 
{
    public class RoomManager : IRoomManager
    {
        private readonly Dictionary<string, Room> _rooms = new();
        public Room CreateRoom(string playerName, int boardSize, string connectionId)
        {
            // Sinh mã phòng
            string roomCode = RoomHelper.GenerateRoomCode(_rooms); 

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

        // Bắt đầu
        public Room StartGame(string roomCode, string connectionId)
        {
            if (!_rooms.TryGetValue(roomCode, out var room))
                throw new HubException("Phòng không tồn tại.");

            if (room.HostConnectionId != connectionId)
                throw new HubException("Chỉ chủ phòng mới được bắt đầu.");

            if (room.Player1 == null || room.Player2 == null)
                throw new HubException("Chưa đủ người chơi.");

            room.IsPlaying = true;

            room.CurrentTurn = 'X';

            return room;
        }

        // Dừng
        public Room StopGame(string roomCode, string connectionId)
        {
            if (!_rooms.TryGetValue(roomCode, out var room))
                throw new HubException("Phòng không tồn tại.");
            if (room.HostConnectionId != connectionId)
                throw new HubException("Chỉ chủ phòng mới được dừng");
            room.IsPlaying = false;
            return room;
        }

        // Đánh lại
        public Room RestartGame(string roomCode, string connectionId)
        {
            if (!_rooms.TryGetValue(roomCode, out var room))
                throw new HubException("Phòng không tồn tại.");

            if (room.HostConnectionId != connectionId)
                throw new HubException("Chỉ chủ phòng mới được đấu lại.");

            // Reset bàn cờ
            room.Board = new char[room.BoardSize, room.BoardSize];
           
            // Xóa các ô chiến thắng
            room.WinningCells.Clear();

            // Trở về trạng thái ban đầu
            room.CurrentTurn = 'X';
            room.IsPlaying = true;

            return room;
        }

        // Nước đi
        public Room? PlacePiece(string roomCode, string connectionId, int row, int col)
        {
            // 1. Kiểm tra phòng tồn tại
            if (!_rooms.TryGetValue(roomCode, out Room? room))
            {
                throw new HubException("Phòng không tồn tại.");
            }
            // 2. Kiểm tra game đã bắt đầu chưa
            if (!room.IsPlaying)
            {
                return room;
            }
            // 3. Xác định người chơi đang đánh (Player1 hay Player2)
            Player? player = null;
            if (room.Player1?.ConnectionId == connectionId)
                player = room.Player1;
            else if (room.Player2?.ConnectionId == connectionId)
                player = room.Player2;

            if (player == null)
                throw new HubException("Bạn không thuộc phòng này.");
            // 4. Kiểm tra đúng lượt hay không
            if (player.Symbol != room.CurrentTurn)
            {
                throw new HubException("Xin thí chủ dừng tay! Chưa đến lượt thí chủ.");
            }
            // 5. Kiểm tra tọa độ có hợp lệ không
            if (row < 0 || row >= room.BoardSize || col < 0 || col >= room.BoardSize)
            {
                throw new HubException("Nước đi này không lường trước được (không hợp lệ).");
            }
            // 6. Kiểm tra ô đã có quân chưa
            if (room.Board[row, col] != '\0')
            {
                throw new HubException("Ô này đã có quân rồi thí chủ ơi!");
            }
            // 7. Đánh dấu quân lên bàn cờ
            room.Board[row, col] = (char)player.Symbol;
            // 8. Kiểm tra thắng/thua
            var winningCells = BoardHelper.CheckWinner(room.Board, row, col);

            if (winningCells != null)
            {
                room.IsPlaying = false;

                room.WinningCells = winningCells;
            }
            else
            {
                // 9. Chuyển lượt
                room.CurrentTurn = room.CurrentTurn == 'X'
                    ? 'O'
                    : 'X';
            }
            // 10. Trả Room để Hub broadcast RoomUpdated
            return room;
        }

     
    }
}
