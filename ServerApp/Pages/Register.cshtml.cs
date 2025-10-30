using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerApp.Models;
using ServerApp.Data;

namespace ServerApp.Pages
{
    public class Register : PageModel
    {
        private readonly ConnectFourContext _context;

        public Register(ConnectFourContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Player Player { get; set; }

        public List<string> Countries { get; set; } = new List<string> { "Israel", "USA", "France", "Germany", "Japan" };

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
                return Page();

            if (_context.Players.Any(p => p.Id == Player.Id))
            {
                ModelState.AddModelError(
                    nameof(Player.Id),
                    "A player with this ID already exists.");
                return Page();
            }

            _context.Players.Add(Player);
            _context.SaveChanges();
            return RedirectToPage("SuccessRegister");
        }

    }
}
