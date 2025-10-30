namespace ServerApp.Models
{
    public class PlayerMovedTo
    {
        public string GameId { get; set; }
        public string PlayerId { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public DateTime MoveTime { get; set; } = DateTime.Now;
    }
}
