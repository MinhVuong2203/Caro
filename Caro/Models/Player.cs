namespace Caro.Models
{
    public class Player
    {
        public string ConnectionId { get; set; } = "";

        public string Name { get; set; } = "";

        public int WinCount { get; set; }

        public int LoseCount { get; set; }

        public int DrawCount { get; set; } // Hòa

        // Avatar
        public string AvatarIcon { get; set; } = "fa-solid fa-user";
        public string AvatarColor { get; set; } = "#131F39";
        public string AvatarAnimation { get; set; } = "";
    }
}
