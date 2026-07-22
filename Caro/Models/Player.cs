namespace Caro.Models
{
    public class Player
    {
        public string ConnectionId { get; set; } = "";

        public string Name { get; set; } = "";

        public char? Symbol { get; set; }   // X hoặc O
    }
}
