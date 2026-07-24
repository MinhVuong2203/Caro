using Caro.DTOs;
using Caro.Interfaces;
using Caro.Models;
using Caro.Utils;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Numerics;
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
            //if (room.Player2 != null) throw new HubException("Phòng đã đầy.");

            Player player = new Player
            {
                ConnectionId = connectionId,
                Name = playerName,
            };
            if (room.Player1 == null)
            {
                room.Player1 = player;  
            }
            else if (room.Player2 == null)
            {
                room.Player2 = player;
            }
            else
            {
                room.Viewers.Add(player);
            }

            return room;
        }

        public Room? Reconnect(string roomCode, string playerName, string connectionId)
        {
            if (!_rooms.TryGetValue(roomCode, out var room)) return null;

            // Nếu là player1 F5
            if (room.Player1?.Name == playerName)
            {
                // Cấp lại connectionId cho Player1 và cập nhật HostConnectionId nếu cần
                var oldConnectionId = room.Player1.ConnectionId;
                room.Player1.ConnectionId = connectionId;
                if (room.HostConnectionId == oldConnectionId)
                {
                    room.HostConnectionId = connectionId;
                }
                return room;
            }
            // Nếu là player2 F5
            if (room.Player2?.Name == playerName)
            {
                var oldConnectionId = room.Player2.ConnectionId;
                room.Player2.ConnectionId = connectionId;
                if (room.HostConnectionId == oldConnectionId)
                {
                    room.HostConnectionId = connectionId;
                }
                return room;
            }
            // Nếu là Viewer F5
            var viewer = room.Viewers.FirstOrDefault(x => x.Name == playerName);
            if (viewer != null)
            {
                var oldId = viewer.ConnectionId;
                viewer.ConnectionId = connectionId;

                if (room.HostConnectionId == oldId)
                    room.HostConnectionId = connectionId;
                return room;
            }
            return null;
        }

        public Room? LeaveRoom(string roomCode, string connectionId)
        {
            if (!_rooms.TryGetValue(roomCode, out var room))
                return null;

            bool isHost = room.HostConnectionId == connectionId;

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

            // Viewer rời
            var viewer = room.Viewers.FirstOrDefault(x => x.ConnectionId == connectionId);
            if (viewer != null)
            {
                room.Viewers.Remove(viewer);
            }

            // Không còn ai -> Xóa phòng
            if (room.Player1 == null &&
                room.Player2 == null &&
                room.Viewers.Count == 0)
            {
                _rooms.Remove(roomCode);
                return null;
            }

            // ==========================
            // Lấp Player1 nếu bị trống
            // ==========================
            if (room.Player1 == null)
            {
                if (room.Player2 != null)
                {
                    room.Player1 = room.Player2;
                    room.Player2 = null;
                }

                if (room.Player2 == null && room.Viewers.Count > 0)
                {
                    room.Player2 = room.Viewers[0];
                    room.Viewers.RemoveAt(0);
                }
            }

            // ==========================
            // Lấp Player2 nếu bị trống
            // ==========================
            if (room.Player2 == null && room.Viewers.Count > 0)
            {
                room.Player2 = room.Viewers[0];
                room.Viewers.RemoveAt(0);
            }

            // ==========================
            // Chuyển Host nếu Host vừa rời
            // ==========================
            if (isHost)
            {
                room.HostConnectionId =
                    room.Player1?.ConnectionId
                    ?? room.Player2?.ConnectionId
                    ?? room.Viewers.First().ConnectionId;
            }

            return room;
        }

        public Room SwapPlayer(string roomCode, string requesterConnectionId, string sourceConnectionId, string targetConnectionId)
        {
            if (!_rooms.TryGetValue(roomCode, out var room))
                throw new HubException("Phòng không tồn tại.");

            if (room.HostConnectionId != requesterConnectionId)
                throw new HubException("Chỉ chủ phòng mới được đổi vị trí.");

            if (room.IsPlaying)
            {
                throw new HubException("Không thể đổi vị trí khi trận đấu đang diễn ra.");
            }

            if (sourceConnectionId == targetConnectionId)
                throw new HubException("Vui lòng chọn hai người khác nhau.");


            var source = RoomHelper.FindPosition(room, sourceConnectionId);
            var target = RoomHelper.FindPosition(room, targetConnectionId);

            var sourcePlayer = RoomHelper.GetPlayer(room, source.Position, source.ViewerIndex);
            var targetPlayer = RoomHelper.GetPlayer(room, target.Position, target.ViewerIndex);

           
            RoomHelper.SetPlayer(room, source.Position, source.ViewerIndex, targetPlayer);
            RoomHelper.SetPlayer(room, target.Position, target.ViewerIndex, sourcePlayer);

            return room;
        }

        public Room ToggleReady(string roomCode, string connectionId)
        {
            if (!_rooms.TryGetValue(roomCode, out var room))
            {
                throw new Exception("Không tìm thấy phòng.");
            }

            bool isHost = room.HostConnectionId == connectionId;

            bool isPlayer1 = room.Player1?.ConnectionId == connectionId;
            bool isPlayer2 = room.Player2?.ConnectionId == connectionId;

            // Viewer không phải Host thì không được thao tác
            if (!isHost && !isPlayer1 && !isPlayer2)
                throw new Exception("Khán giả thì ngồi im!");

            // ======================
            // Đang chơi -> Dừng game
            // ======================
            if (room.IsPlaying && room.WinningCells.Count == 0)
            {
                room.IsPlaying = false;
                return room;
            }

            // ======================
            // Host đang là Viewer
            // Không có trạng thái Ready
            // ======================
            if (!isPlayer1 && !isPlayer2)
                return room;

            // ======================
            // Toggle Ready
            // ======================
            if (isPlayer1)
                room.Player1Ready = !room.Player1Ready;

            if (isPlayer2)
                room.Player2Ready = !room.Player2Ready;

            // Chưa đủ người
            if (room.Player1 == null || room.Player2 == null)
                return room;

            // Chưa cùng Ready
            if (!room.Player1Ready || !room.Player2Ready)
                return room;

            // ======================
            // Hai người cùng Ready
            // ======================

            room.Player1Ready = false;
            room.Player2Ready = false;

            room.IsPlaying = true;
            //room.CurrentTurn = 'X';

            // Nếu ván trước đã kết thúc thì reset
            if (room.WinningCells.Count > 0)
            {
                room.Board = new char[room.BoardSize, room.BoardSize];
                room.WinningCells.Clear();
                room.LastMove = null;
                room.DrawRequesterConnectionId = null;
                room.CurrentTurn = 'X';
            }

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
            {
                player = room.Player1;
            }
            else if (room.Player2?.ConnectionId == connectionId)
            {
                player = room.Player2;
            }
            else if (room.Viewers.Any(v => v.ConnectionId == connectionId))
            {
                throw new HubException("Khán giả không được nhúng tay vào trận đấu!");
            }
            else
            {
                throw new HubException("Bạn không thuộc phòng này.");
            }
            //// 4. Kiểm tra đúng lượt hay không
            char symbol = room.Player1?.ConnectionId == connectionId ? 'X' : 'O';
            if (symbol != room.CurrentTurn)
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
            room.Board[row, col] = symbol;
            room.LastMove = new Position
                { 
                    Row = row, 
                    Col = col
                };
            // 8. Kiểm tra thắng/thua
            var winningCells = BoardHelper.CheckWinner(room.Board, row, col);

            if (winningCells != null)
            {
                room.IsPlaying = false;
                room.WinningCells = winningCells;
                if (room.CurrentTurn == 'X')
                {
                    room.Player1!.WinCount++;
                    room.Player2!.LoseCount++;
                }
                else
                {
                    room.Player2!.WinCount++;
                    room.Player1!.LoseCount++;
                }
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

        // yêu cầu hòa
        public Room RequestDraw(string roomCode, string connectionId)
        {
            if (!_rooms.TryGetValue(roomCode, out Room? room))
            {
                throw new HubException("Phòng không tồn tại.");
            }

            if (!room.IsPlaying)
                throw new Exception("Ván đấu chưa bắt đầu.");

            if (room.DrawRequesterConnectionId != null)
                throw new Exception("Đã có một yêu cầu hòa đang chờ.");

            bool isPlayer =
                room.Player1?.ConnectionId == connectionId ||
                room.Player2?.ConnectionId == connectionId;

            if (!isPlayer)
                throw new Exception("Viewer không thể yêu cầu hòa.");

            room.DrawRequesterConnectionId = connectionId;

            return room;
        }

        public Room AcceptDraw(string roomCode, string connectionId)
        {
            if (!_rooms.TryGetValue(roomCode, out Room? room))
            {
                throw new HubException("Phòng không tồn tại.");
            }

            if (room.DrawRequesterConnectionId == null)
                throw new Exception("Không có yêu cầu hòa.");

            if (room.DrawRequesterConnectionId == connectionId)
                throw new Exception("Bạn không thể tự chấp nhận yêu cầu hòa.");

            room.Player1!.DrawCount++;
            room.Player2!.DrawCount++;

            room.IsPlaying = false;
            room.WinningCells.Clear();
            room.DrawRequesterConnectionId = null;
            room.Board = new char[room.BoardSize, room.BoardSize];
            room.LastMove = null;
            room.CurrentTurn = 'X';

            return room;
        }

        public Room RejectDraw(string roomCode, string connectionId)
        {
            if (!_rooms.TryGetValue(roomCode, out Room? room))
            {
                throw new HubException("Phòng không tồn tại.");
            }

            if (room.DrawRequesterConnectionId == null)
                throw new Exception("Không có yêu cầu hòa.");

            if (room.DrawRequesterConnectionId == connectionId)
                throw new Exception("Bạn không thể tự từ chối yêu cầu hòa.");

            room.DrawRequesterConnectionId = null;

            return room;
        }

        public Room UpdateAvatar(UpdateAvatarRequest request, string connectionId)
        {
            if (!_rooms.TryGetValue(request.RoomCode, out var room))
                throw new Exception("Không tìm thấy phòng.");

            Player? player = null;

            if (room.Player1?.ConnectionId == connectionId)
                player = room.Player1;
            else if (room.Player2?.ConnectionId == connectionId)
                player = room.Player2;
            else
                player = room.Viewers.FirstOrDefault(v => v.ConnectionId == connectionId);

            if (player == null)
                throw new Exception("Không tìm thấy người chơi.");

            player.AvatarIcon = request.Icon;
            player.AvatarAnimation = request.Animation;
            player.AvatarColor = request.Color;

            return room;
        }

    }
}
