using Caro.Models;

namespace Caro.Interfaces
{
    public interface IRoomManager
    {
        Room CreateRoom(string playerName, int boardSize, string connectionId);

        Room? JoinRoom(string roomCode, string playerName, string connectionId);
        Room? Reconnect(string roomCode, string playerName, string connnectionId);
        Room? LeaveRoom(string roomCode, string connectionId);
    }
}
