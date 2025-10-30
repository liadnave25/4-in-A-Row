using Microsoft.AspNetCore.Mvc;
using ServerApp.Data;
using ServerApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerApp.WebApiCoreManagement
{
    // DTO for incoming move and game requests
    public class MoveDto
    {
        public int GameId { get; set; }
        public int PlayerId { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ConnectFourApiController : ControllerBase
    {
        private readonly ConnectFourContext _context;
        private const int Rows = 6;
        private const int Columns = 7;

        public ConnectFourApiController(ConnectFourContext context)
        {
            _context = context;
        }

        // GET api/ConnectFourApi/games
        [HttpGet("games")]
        public ActionResult<IEnumerable<Game>> GetAllGames()
        {
            var games = _context.Games.ToList();
            return Ok(games);
        }

        // GET api/ConnectFourApi/nextGame/{playerId}
        [HttpGet("nextGame/{playerId}")]
        public ActionResult<int> NextGame(int playerId)
        {
            if (!_context.Players.Any(p => p.Id == playerId))
                return NotFound(new { message = "Player not found." });

            var game = new Game
            {
                PlayerId = playerId,
                StartTime = DateTime.UtcNow,
                Moves = "0",
                Duration = TimeSpan.Zero,
                Winner = null
            };
            _context.Games.Add(game);
            _context.SaveChanges();

            return Ok(game.Id);
        }

        // GET api/ConnectFourApi/moves/{gameId}
        [HttpGet("moves/{gameId}")]
        public ActionResult<IEnumerable<Move>> GetMovesForGame(int gameId)
        {
            if (!_context.Games.Any(g => g.Id == gameId))
                return NotFound(new { message = "Game not found." });

            var moves = _context.Moves
                                .Where(m => m.GameId == gameId)
                                .OrderBy(m => m.MoveTime)
                                .ToList();

            return Ok(moves);
        }

        // POST api/ConnectFourApi/move
        [HttpPost("move")]
        public ActionResult<object> SubmitMove([FromBody] MoveDto dto)
        {
            // Validate player and game
            if (!_context.Players.Any(p => p.Id == dto.PlayerId))
                return NotFound(new { message = "Player not found." });
            if (!_context.Games.Any(g => g.Id == dto.GameId))
                return NotFound(new { message = "Game not found." });

            // Save the player's move
            var playerMove = new Move
            {
                GameId = dto.GameId,
                PlayerId = dto.PlayerId,
                Row = dto.Row,
                Column = dto.Column,
                MoveTime = DateTime.UtcNow
            };
            _context.Moves.Add(playerMove);
            _context.SaveChanges();

            // Get the game entity for updates
            var game = _context.Games.First(g => g.Id == dto.GameId);

            // ---- 1. CHECK IF PLAYER WON ----
            if (CheckForWinner(dto.GameId, dto.PlayerId))
            {
                game.Duration = DateTime.UtcNow - game.StartTime;
                game.Winner = $"Player {dto.PlayerId} win";
                _context.SaveChanges();
                return Ok(new
                {
                    result = "PlayerWin",
                    yourMove = new { row = dto.Row, column = dto.Column },
                    opponentMove = (object)null
                });
            }

            // ---- 2. CHECK IF BOARD IS FULL (DRAW) ----
            if (BoardIsFull(dto.GameId))
            {
                game.Duration = DateTime.UtcNow - game.StartTime;
                game.Winner = "No winner";
                _context.SaveChanges();
                return Ok(new
                {
                    result = "Draw",
                    yourMove = new { row = dto.Row, column = dto.Column },
                    opponentMove = (object)null
                });
            }

            // ---- 3. COMPUTER/Opponent MOVE ----
            var rand = new Random();
            int oppCol;
            do
            {
                oppCol = rand.Next(0, Columns);
            } while (_context.Moves.Count(m => m.GameId == dto.GameId && m.Column == oppCol) >= Rows);

            // Compute opponent row
            var usedRows = _context.Moves
                .Where(m => m.GameId == dto.GameId && m.Column == oppCol)
                .Select(m => m.Row)
                .ToHashSet();
            int oppRow = Enumerable.Range(0, Rows)
                .Reverse()
                .First(r => !usedRows.Contains(r));

            // Persist opponent move
            var compMove = new Move
            {
                GameId = dto.GameId,
                PlayerId = dto.PlayerId == 1 ? 2 : 1,
                Row = oppRow,
                Column = oppCol,
                MoveTime = DateTime.UtcNow
            };
            _context.Moves.Add(compMove);
            _context.SaveChanges();

            // ---- 4. CHECK IF COMPUTER WON ----
            int computerId = compMove.PlayerId;
            if (CheckForWinner(dto.GameId, computerId))
            {
                game.Duration = DateTime.UtcNow - game.StartTime;
                game.Winner = "Computer win";
                _context.SaveChanges();
                return Ok(new
                {
                    result = "ComputerWin",
                    yourMove = new { row = dto.Row, column = dto.Column },
                    opponentMove = new { row = oppRow, column = oppCol }
                });
            }

            // ---- 5. CHECK IF BOARD IS FULL (DRAW) ----
            if (BoardIsFull(dto.GameId))
            {
                game.Duration = DateTime.UtcNow - game.StartTime;
                game.Winner = "No winner";
                _context.SaveChanges();
                return Ok(new
                {
                    result = "Draw",
                    yourMove = new { row = dto.Row, column = dto.Column },
                    opponentMove = new { row = oppRow, column = oppCol }
                });
            }

            // ---- 6. If game still ongoing, return both moves ----
            return Ok(new
            {
                result = "Bambi"
//                yourMove = new { row = dto.Row, column = dto.Column },
//                opponentMove = new { row = oppRow, column = oppCol }
            });
        }

        private bool CheckForWinner(int gameId, int playerId)
        {
            // Build board from DB moves
            var moves = _context.Moves.Where(m => m.GameId == gameId).ToList();
            int[,] board = new int[Rows, Columns];

            foreach (var move in moves)
                board[move.Row, move.Column] = move.PlayerId;

            // Horizontal
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c <= Columns - 4; c++)
                    if (board[r, c] == playerId && board[r, c + 1] == playerId && board[r, c + 2] == playerId && board[r, c + 3] == playerId)
                        return true;
            // Vertical
            for (int c = 0; c < Columns; c++)
                for (int r = 0; r <= Rows - 4; r++)
                    if (board[r, c] == playerId && board[r + 1, c] == playerId && board[r + 2, c] == playerId && board[r + 3, c] == playerId)
                        return true;
            // Diagonal down-right
            for (int r = 0; r <= Rows - 4; r++)
                for (int c = 0; c <= Columns - 4; c++)
                    if (board[r, c] == playerId && board[r + 1, c + 1] == playerId && board[r + 2, c + 2] == playerId && board[r + 3, c + 3] == playerId)
                        return true;
            // Diagonal down-left
            for (int r = 0; r <= Rows - 4; r++)
                for (int c = 3; c < Columns; c++)
                    if (board[r, c] == playerId && board[r + 1, c - 1] == playerId && board[r + 2, c - 2] == playerId && board[r + 3, c - 3] == playerId)
                        return true;
            return false;
        }
        private bool BoardIsFull(int gameId)
        {
            var moves = _context.Moves.Where(m => m.GameId == gameId).ToList();
            return moves.Count >= Rows * Columns;
        }


        private bool ColumnIsFull(int gameId, int column)
        {
            return _context.Moves.Count(m => m.GameId == gameId && m.Column == column) >= Rows;
        }
    }
}
