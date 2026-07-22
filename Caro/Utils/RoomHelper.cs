using Caro.Models;

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
    }
}
