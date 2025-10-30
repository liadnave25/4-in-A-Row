namespace ServerApp.Models
{
    public class Move
    {
        public int Id { get; set; } // מפתח ראשי אוטומטי

        public int GameId { get; set; }
        public int PlayerId { get; set; }

        public int Row { get; set; }
        public int Column { get; set; }

        public DateTime MoveTime { get; set; } = DateTime.Now;
    }
}
