namespace Caro.Models
{
    public class Room
    {
        public string RoomCode { get; set; } = "";

        public Player? Player1 { get; set; }

        public Player? Player2 { get; set; }
        public string HostConnectionId { get; set; } = "";
        public int BoardSize { get; set; }

        public char[,] Board { get; set; } = default!;

        public char CurrentTurn { get; set; } = 'X';

        public bool IsPlaying { get; set; }
        public List<Position> WinningCells { get; set; } = [];
        public Position? LastMove { get; set; }
    }
}
