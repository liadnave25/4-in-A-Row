using System;
using System.Windows.Forms;

namespace ClientApp
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            int gameId = args.Length > 0 ? int.Parse(args[0]) : -1;
            int playerId = args.Length > 1 ? int.Parse(args[1]) : -1;

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1(gameId, playerId));
        }

    }
}
