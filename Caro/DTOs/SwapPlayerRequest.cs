namespace Caro.DTOs
{
    public class SwapPlayerRequest
    {
        public string RoomCode { get; set; } = "";

        public string SourceConnectionId { get; set; } = "";

        public string TargetConnectionId { get; set; } = "";
    }
}
