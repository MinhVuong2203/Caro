namespace Caro.DTOs
{
    public class KickPlayerRequest
    {
        public string RoomCode { get; set; } = "";
        public string TargetConnectionId { get; set; } = "";
    }
}
