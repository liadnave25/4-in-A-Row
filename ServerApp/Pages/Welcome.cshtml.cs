using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using ServerApp.Data;
using ServerApp.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ServerApp.Pages
{
    public class WelcomeModel : PageModel
    {
        private readonly ConnectFourContext _context;
        private readonly IConfiguration _config;

        public WelcomeModel(ConnectFourContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; } // PlayerId

        public IActionResult OnPostPlay()
        {
            var newGame = new Game
            {
                PlayerId = Id,
                StartTime = DateTime.UtcNow,
                Duration = TimeSpan.Zero,
                Moves = string.Empty,
                Winner = null
            };
            _context.Games.Add(newGame);
            _context.SaveChanges();

            int gameId = newGame.Id;

            var exePath = GetClientAppExePath();
            if (exePath == null)
            {

                ModelState.AddModelError(string.Empty,
                    "ClientApp.exe לא נמצא. בדוק שהרצת Build לפרויקט ClientApp או הגדרת הנתיב ב-appsettings.json.");
                return Page();
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"{gameId} {Id}",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $": {ex.Message}");
            }

            return Page();
        }


        private string? GetClientAppExePath()
        {
            var custom = _config["ClientApp:CustomExePath"];
            if (!string.IsNullOrWhiteSpace(custom) && System.IO.File.Exists(custom))
                return custom;

            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (int i = 0; i < 8; i++)
            {
                var candidate = Path.Combine(
                    dir.FullName,
                    "ClientApp", "bin", "Debug", "net8.0-windows", "ClientApp.exe");
                if (System.IO.File.Exists(candidate))
                    return candidate;
                dir = dir.Parent ?? dir;
            }

            return null;
        }
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            // 1. Sign the user out (removes the auth cookie)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. Redirect back to the public Home page
            return RedirectToPage("Index");
        }
    }
}
