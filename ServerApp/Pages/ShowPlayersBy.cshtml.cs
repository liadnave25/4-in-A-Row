using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerApp.Data;
using ServerApp.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace ServerApp.Pages
{
    [Authorize]
    public class ShowPlayersByModel : PageModel
    {
        private readonly ConnectFourContext _context;
        public List<Player> Players { get; set; } = new();
        public List<Game> Games { get; set; } = new();
        public int? SelectedPlayerId { get; set; }
        public Player? SelectedPlayer { get; set; }
        public string SortOrder { get; set; } = "asc";
        public List<Game> AllGames { get; set; } = new();
        public List<(string PlayerName, DateTime? LastGame)> PlayersWithLastGame { get; set; } = new();
        public List<(string PlayerName, int GameCount)> PlayersWithGameCount { get; set; } = new(); // ← חדש עבור 1.7.6
        public List<(string Country, int Count)> PlayersPerCountry { get; set; } = new();

        public ShowPlayersByModel(ConnectFourContext context)
        {
            _context = context;
        }

        public void OnGet(string? sortOrder, int? selectedPlayerId)
        {
            SortOrder = sortOrder ?? "asc";
            SelectedPlayerId = selectedPlayerId;

            var query = _context.Players.AsQueryable();

            if (SortOrder == "desc")
                query = query.OrderByDescending(p => p.FirstName.ToLower());
            else
                query = query.OrderBy(p => p.FirstName.ToLower());

            Players = query.ToList();


            if (SelectedPlayerId.HasValue)
            {
                SelectedPlayer = _context.Players.FirstOrDefault(p => p.Id == SelectedPlayerId.Value);
                Games = _context.Games
                    .Where(g => g.PlayerId == SelectedPlayerId.Value)
                    .OrderByDescending(g => g.StartTime)
                    .ToList();
            }

            PlayersWithLastGame = _context.Players
                .Select(p => new
                {
                    Name = p.FirstName,
                    LastGame = _context.Games
                        .Where(g => g.PlayerId == p.Id)
                        .OrderByDescending(g => g.StartTime)
                        .Select(g => (DateTime?)g.StartTime)
                        .FirstOrDefault()
                })
                .AsEnumerable()
                .Select(p => (p.Name, p.LastGame))
                .ToList();

            // ✅ 1.7.6 – מיון לפי מספר משחקים
            PlayersWithGameCount = _context.Players
                .Select(p => new
                {
                    Name = p.FirstName,
                    GameCount = _context.Games.Count(g => g.PlayerId == p.Id)
                })
                .OrderByDescending(p => p.GameCount)
                .AsEnumerable()
                .Select(p => (p.Name, p.GameCount))
                .ToList();

            PlayersPerCountry = _context.Players
                .GroupBy(p => p.Country)
                .Select(g => new { Country = g.Key, Count = g.Count() })
                .AsEnumerable()
                .Select(g => (g.Country ?? "Unknown", g.Count))
                .ToList();

            AllGames = _context.Games
                .GroupBy(g => g.Id)
                .Select(g => g.First())
                .ToList();
        }
    }
}
