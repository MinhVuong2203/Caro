namespace Caro.DTOs
{
    public class PlacePieceRequest
    {
        public string RoomCode { get; set; } = "";
        public int Row { get; set; }
        public int Col { get; set; }
    }
}
