using Caro.DTOs;
using Caro.Models;

namespace Caro.Interfaces
{
    public interface IRoomManager
    {
        Room CreateRoom(string playerName, int boardSize, string connectionId);

        Room? JoinRoom(string roomCode, string playerName, string connectionId);
        Room? Reconnect(string roomCode, string playerName, string connnectionId);
        Room? LeaveRoom(string roomCode, string connectionId);
        Room SwapPlayer(string roomCode, string requesterConnectionId, string sourceConnectionId, string targetConnectionId);
        Room ToggleReady(string roomCode, string connectionId);
        Room? PlacePiece(string roomCode, string connectionId, int row, int col);
        Room RequestDraw(string roomCode, string connectionId);
        Room AcceptDraw(string roomCode, string connectionId);
        Room RejectDraw(string roomCode, string connectionId);
        Room UpdateAvatar(UpdateAvatarRequest request, string connectionId);
    }
}
