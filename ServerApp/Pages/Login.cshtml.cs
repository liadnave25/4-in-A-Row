using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServerApp.Data;
using ServerApp.Models;

namespace ServerApp.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ConnectFourContext _context;

        public LoginModel(ConnectFourContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int PlayerId { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Verify user exists
            var player = _context.Players.FirstOrDefault(p => p.Id == PlayerId);
            if (player == null)
            {
                ErrorMessage = "Player not found. Please register first.";
                return Page();
            }

            // 2. Create the user claims & cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, PlayerId.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            // 3. Redirect to Welcome (same as before)
            return RedirectToPage("Welcome", new { id = PlayerId });
        }
    }
}
