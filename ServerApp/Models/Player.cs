using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServerApp.Models
{
    public class Player
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; } 

        [Required]
        [MinLength(2)]
        public string FirstName { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        [Required]
        public string Country { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public ICollection<Game> Games { get; set; } = new List<Game>();
    }
}
