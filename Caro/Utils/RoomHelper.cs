using Caro.Models;
using Microsoft.AspNetCore.SignalR;

namespace Caro.Utils
{
    public static class RoomHelper
    {
        public static string GenerateRoomCode(Dictionary<string, Room> _rooms)
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

        public static (string Position, int ViewerIndex) FindPosition(Room room, string connectionId)
        {
            if (room.Player1?.ConnectionId == connectionId)
                return ("Player1", -1);

            if (room.Player2?.ConnectionId == connectionId)
                return ("Player2", -1);

            int index = room.Viewers.FindIndex(v => v.ConnectionId == connectionId);

            if (index != -1)
                return ("Viewer", index);

            throw new HubException("Không tìm thấy người chơi.");
        }

        public static Player GetPlayer(Room room, string position, int viewerIndex)
        {
            return position switch
            {
                "Player1" => room.Player1!,
                "Player2" => room.Player2!,
                "Viewer" => room.Viewers[viewerIndex],
                _ => throw new HubException("Vị trí không hợp lệ.")
            };
        }

        public static void SetPlayer(Room room, string position, int viewerIndex, Player player)
        {
            switch (position)
            {
                case "Player1":
                    room.Player1 = player;
                    break;

                case "Player2":
                    room.Player2 = player;
                    break;

                case "Viewer":
                    room.Viewers[viewerIndex] = player;
                    break;
            }
        }
    }
}
