using Microsoft.EntityFrameworkCore;
using ServerApp.Models;
using System.Collections.Generic;

namespace ServerApp.Data
{
    public class ConnectFourContext : DbContext
    {
        public ConnectFourContext(DbContextOptions<ConnectFourContext> options) : base(options) { }

        public DbSet<Player> Players { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Move> Moves { get; set; }
    }
}
