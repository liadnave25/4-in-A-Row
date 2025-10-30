using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using ServerApp.Data;
using ServerApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace ServerApp.Pages
{
    [Authorize]
    public class EditPlayerModel : PageModel
    {
        private readonly ConnectFourContext _context;
        [BindProperty]
        public Player Player { get; set; }

        public EditPlayerModel(ConnectFourContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Player = await _context.Players.FindAsync(id);
            if (Player == null)
                return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var existing = await _context.Players.FindAsync(Player.Id);
            if (existing == null)
                return NotFound();

            existing.FirstName = Player.FirstName;
            existing.Phone = Player.Phone;
            existing.Country = Player.Country;

            await _context.SaveChangesAsync();
            return RedirectToPage("/ShowPlayersBy");
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var player = await _context.Players
                .Include(p => p.Games)
                .FirstOrDefaultAsync(p => p.Id == Player.Id);
            if (player == null)
                return NotFound();

            if (player.Games != null)
            {
                _context.Games.RemoveRange(player.Games);
            }

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync();

            return RedirectToPage("/Index");
        }
    }
}
