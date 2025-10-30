using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServerApp.Data;
using ServerApp.Models;

namespace ServerApp.Pages
{
    [Authorize]
    public class GamesModel : PageModel
    {
        private readonly ConnectFourContext _context;
        public List<Game> Games { get; set; } = new();

        // Bound property for deletion
        [BindProperty]
        public int GameId { get; set; }

        public GamesModel(ConnectFourContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            Games = await _context.Games
                .Where(g => g.PlayerId == userId)
                .OrderByDescending(g => g.StartTime)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Reload list for display
            Games = await _context.Games
                .Where(g => g.PlayerId == userId)
                .ToListAsync();

            if (!Games.Any(g => g.Id == GameId))
            {
                ModelState.AddModelError(nameof(GameId), "Write a correct game id");
                return Page();
            }

            var game = await _context.Games.FindAsync(GameId);
            _context.Games.Remove(game!);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
