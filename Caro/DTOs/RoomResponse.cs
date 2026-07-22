using Caro.Models;

namespace Caro.DTOs
{
    public class RoomResponse
    {
        public string RoomCode { get; set; } = "";
        public Player? Player1 { get; set; }
        public Player? Player2 { get; set; }
        public string HostConnectionId { get; set; } = "";
        public int BoardSize { get; set; }
        public char CurrentTurn { get; set; } = 'X';
        public bool IsPlaying { get; set; }
        public string[][] Board { get; set; } = [];
        public List<Position> WinningCells { get; set; } = [];
    }
}
