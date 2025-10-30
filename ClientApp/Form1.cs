using ServerApp.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace ClientApp
{
    public partial class Form1 : Form
    {
        private readonly HttpClient httpClient;
        private int gameId;
        private readonly int playerId;
        private readonly int myNumber = 1;
        private readonly int opponentNumber = 2;

        private const int Rows = 6;
        private const int Columns = 7;
        private const int CellSize = 80;
        private const int MarginSize = 20;

        private bool isMyTurn = true;
        private bool gameOver = false;
        private bool isDropping = false;
        private bool isOpponentDropping = false;

        private int dropCol;
        private int dropRow;
        private int targetRow;

        private int opponentDropCol;
        private int opponentDropRow;
        private int opponentTargetRow;

        private int boardOriginX;
        private int boardOriginY;
        private Rectangle[,] boardRects = new Rectangle[Rows, Columns];
        private int[,] board = new int[Rows, Columns];

        private List<Move> restoredMoves = new();
        private int restoreIndex = 0;

        private readonly System.Windows.Forms.Timer dropTimer = new() { Interval = 50 };
        private readonly System.Windows.Forms.Timer restoreTimer = new() { Interval = 500 };

        // UI controls
        private readonly ComboBox comboGameId = new() { Width = 120 };
        private readonly Button btnRestoreGame = new() { Text = "Load" };
        private readonly Button btnRestart = new() { Text = "Restart" };
        private readonly Label lblTurn = new();
        private readonly Label lblResult = new();
        private readonly Panel circlePlayer1 = new() { Size = new Size(20, 20) };
        private readonly Panel circlePlayer2 = new() { Size = new Size(20, 20) };
        private readonly Label lblPlayer1 = new();
        private readonly Label lblPlayer2 = new();

        public Form1(int gameId, int playerId)
        {
            InitializeComponent();
            this.gameId = gameId;
            this.playerId = playerId;

            // Enable double buffering to reduce flicker
            DoubleBuffered = true;
            InitializeBoard();

            // Set window title
            Text = $"Player #{playerId} - Game #{gameId} - Connect Four";

            // Configure HttpClient for development
            var handler = new HttpClientHandler
            {
                // Accept self-signed SSL cert for local development
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5136/api/ConnectFourApi/")
            };
            httpClient.DefaultRequestHeaders.Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Build UI
            SetupUI();

            // Timer event handlers
            dropTimer.Tick += DropTimer_Tick;
            restoreTimer.Tick += RestoreTimer_Tick;

            // Load available game IDs for restore
            LoadGameIds();
        }

        private void SetupUI()
        {
            // 1) combo + Load at top-left
            comboGameId.Location = new Point(MarginSize, MarginSize);
            Controls.Add(comboGameId);

            btnRestoreGame.Location = new Point(comboGameId.Right + 10, MarginSize);
            btnRestoreGame.Click += BtnRestoreGame_Click;
            Controls.Add(btnRestoreGame);
            // 2b) Restart button next to Load

            btnRestart.Location = new Point(btnRestoreGame.Right + 10, MarginSize);
            btnRestart.Click += BtnRestart_Click;
            Controls.Add(btnRestart);


            // 2) Header, centered on the same top Y
            lblTurn.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTurn.AutoSize = true;
            lblTurn.Text = $"Your Turn (Player {myNumber})";
            // measure text width so we can center it
            Size hdrSize = TextRenderer.MeasureText(lblTurn.Text, lblTurn.Font);
            int hdrX = (ClientSize.Width - hdrSize.Width) / 2;
            lblTurn.Size = hdrSize;
            lblTurn.Location = new Point(hdrX, MarginSize);
            Controls.Add(lblTurn);


            // 3) Indicators, immediately under header
            const int circleSize = 20;
            const int gap = 5;
            const int groupSpacing = 30;

            // prepare labels
            lblPlayer1.Font = lblTurn.Font; // same font so heights align nicely
            lblPlayer1.AutoSize = true;
            lblPlayer1.Text = $"You (Player {myNumber})";
            Size p1Size = TextRenderer.MeasureText(lblPlayer1.Text, lblPlayer1.Font);
            lblPlayer1.Size = p1Size;

            lblPlayer2.Font = lblTurn.Font;
            lblPlayer2.AutoSize = true;
            lblPlayer2.Text = $"Computer (Player {opponentNumber})";
            Size p2Size = TextRenderer.MeasureText(lblPlayer2.Text, lblPlayer2.Font);
            lblPlayer2.Size = p2Size;

            // compute total width of: [circle+gap+label1] + spacing + [circle+gap+label2]
            int totalWidth = (circleSize + gap + p1Size.Width)
                           + groupSpacing
                           + (circleSize + gap + p2Size.Width);
            int groupX = (ClientSize.Width - totalWidth) / 2;
            int indicatorY = lblTurn.Bottom + 10;

            // circle 1
            circlePlayer1.Size = new Size(circleSize, circleSize);
            circlePlayer1.Location = new Point(groupX, indicatorY);
            circlePlayer1.Paint += (s, e) => e.Graphics.FillEllipse(Brushes.Red, 0, 0, circleSize, circleSize);
            Controls.Add(circlePlayer1);
            // label 1
            lblPlayer1.Location = new Point(circlePlayer1.Right + gap,
                                               indicatorY + (circleSize - p1Size.Height) / 2);
            Controls.Add(lblPlayer1);

            // circle 2
            int secondX = lblPlayer1.Right + groupSpacing;
            circlePlayer2.Size = new Size(circleSize, circleSize);
            circlePlayer2.Location = new Point(secondX, indicatorY);
            circlePlayer2.Paint += (s, e) => e.Graphics.FillEllipse(Brushes.Yellow, 0, 0, circleSize, circleSize);
            Controls.Add(circlePlayer2);
            // label 2
            lblPlayer2.Location = new Point(circlePlayer2.Right + gap,
                                               indicatorY + (circleSize - p2Size.Height) / 2);
            Controls.Add(lblPlayer2);

            // 4) Result label (you can keep this at left under the combo/button,
            //    or move it anywhere you like; here it stays at left under Load)
            lblResult.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblResult.AutoSize = true;
            lblResult.Location = new Point(MarginSize, comboGameId.Bottom + 10);
            Controls.Add(lblResult);
        }

        private async void LoadGameIds()
        {
            try
            {
                // Build and show the full URL we’re calling
                string endpoint = "api/ConnectFourApi/games";
                var fullUri = new Uri(httpClient.BaseAddress, endpoint);
                MessageBox.Show($"Calling: {fullUri}", "DEBUG");

                // Perform the request
                var games = await httpClient.GetFromJsonAsync<List<Game>>("games");
                comboGameId.Items.Clear();
                if (games != null)
                {
                    foreach (var g in games)
                        comboGameId.Items.Add(g.Id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading games: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }



        private async void BtnRestoreGame_Click(object? sender, EventArgs e)
        {
            if (comboGameId.SelectedItem is not int selId)
            {
                MessageBox.Show("Please select a GameId to restore.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // === DEBUG: show exactly what we're calling ===
            var endpoint = $"moves/{selId}";
            var fullUri = new Uri(httpClient.BaseAddress, endpoint);
            MessageBox.Show($"Calling: {fullUri}", "DEBUG");
            try
            {
                var response = await httpClient.GetAsync($"moves/{selId}");
                response.EnsureSuccessStatusCode();
                var moves = await response.Content.ReadFromJsonAsync<List<Move>>();
                if (moves == null || moves.Count == 0)
                {
                    MessageBox.Show("No moves found for this game.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                restoredMoves = moves;
                restoreIndex = 0;
                ClearBoardContents();
                restoreTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error restoring game: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnRestart_Click(object sender, EventArgs e)
        {
            bool mid = !IsBoardEmpty() && !IsGameFinished();
            if (mid)
            {
                var res = MessageBox.Show(
                    "You are in the middle of a game. Are you sure you want to restart?",
                    "Confirm Restart",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (res != DialogResult.Yes)
                    return;
            }
            await RestartGameAsync();
        }
        private bool IsBoardEmpty()
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Columns; c++)
                    if (board[r, c] != 0) return false;
            return true;
        }

        private bool IsGameFinished()
            => CheckWin(myNumber) || CheckWin(opponentNumber) || IsBoardFull();

        private void ClearBoardContents()
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Columns; c++)
                    board[r, c] = 0;
            Invalidate();
        }
        private async Task RestartGameAsync()
        {
            // 1) Stop & reset any ongoing animations or restore
            dropTimer.Stop();
            restoreTimer.Stop();
            isDropping = false;
            isOpponentDropping = false;
            dropRow = dropCol = targetRow = 0;
            opponentDropRow = opponentDropCol = opponentTargetRow = 0;
            restoredMoves.Clear();
            restoreIndex = 0;

            // 2) Always create a brand-new game on the server
            gameId = await httpClient.GetFromJsonAsync<int>($"nextGame/{playerId}");
            Text = $"Player #{playerId} - Game #{gameId} - Connect Four";

            // 3) Reset turn & result UI
            isMyTurn = true;
            lblTurn.Text = $"Your Turn (Player {myNumber})";
            lblResult.Text = string.Empty;

            // Reset the game‐over flag so the next session is live
            gameOver = false;

            // 4) Blank out the board
            ClearBoardContents();
            LoadGameIds();
        }


        private void InitializeBoard()
        {
            // center the board under the indicators
            int boardWidth = Columns * CellSize;
            int boardHeight = Rows * CellSize;

            // horizontally centered
            boardOriginX = (ClientSize.Width - boardWidth) / 2;
            // vertically placed just below the indicators + label2
            boardOriginY = lblPlayer2.Bottom + MarginSize + 60;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    boardRects[r, c] = new Rectangle(
                        boardOriginX + c * CellSize,
                        boardOriginY + r * CellSize,
                        CellSize,
                        CellSize);
                    board[r, c] = 0;
                }
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Columns; c++)
                {
                    var rect = boardRects[r, c];
                    g.FillRectangle(Brushes.LightBlue, rect);
                    g.DrawRectangle(Pens.Black, rect);
                    switch (board[r, c])
                    {
                        case 1: g.FillEllipse(Brushes.Red, rect); break;
                        case 2: g.FillEllipse(Brushes.Yellow, rect); break;
                        default: g.FillEllipse(Brushes.White, rect); break;
                    }
                }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (!isMyTurn || isDropping || isOpponentDropping || restoreTimer.Enabled)
                return;
            if (gameOver)
            {
                MessageBox.Show(
                  "Game is already over.",
                  "Info",
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Information);
                return;
            }
            for (int c = 0; c < Columns; c++)
            {
                var topRect = boardRects[0, c];
                if (e.X >= topRect.Left && e.X <= topRect.Right)
                    for (int r = Rows - 1; r >= 0; r--)
                        if (board[r, c] == 0)
                        {
                            dropCol = c;
                            dropRow = 0;
                            targetRow = r;
                            isDropping = true;
                            dropTimer.Start();
                            return;
                        }
            }
        }

        private void DropTimer_Tick(object? sender, EventArgs e)
        {
            // User drop animation
            if (isDropping)
            {
                if (dropRow > 0)
                    board[dropRow - 1, dropCol] = 0;
                board[dropRow, dropCol] = myNumber;
                Invalidate();

                if (dropRow == targetRow)
                {
                    dropTimer.Stop();
                    isDropping = false;

                    if (CheckWin(myNumber))
                    {
                        SendMoveToServer(dropRow, dropCol, animateOpponent: false);
                        ShowResult("You win!", Color.Green);
                        return;
                    }
                    if (IsBoardFull())
                    {
                        SendMoveToServer(dropRow, dropCol, animateOpponent: false);
                        ShowResult("It's a draw!", Color.DarkOrange);
                        return;
                    }

                    // 2) Otherwise, record + animate the opponent as normal:
                    SendMoveToServer(dropRow, dropCol, animateOpponent: true);
                    lblTurn.Text = "Opponent's Turn...";
                }
                else
                {
                    dropRow++;
                }
            }
            // Opponent drop animation
            else if (isOpponentDropping)
            {
                if (opponentDropRow > 0)
                    board[opponentDropRow - 1, opponentDropCol] = 0;
                board[opponentDropRow, opponentDropCol] = opponentNumber;
                Invalidate();

                if (opponentDropRow == opponentTargetRow)
                {
                    dropTimer.Stop();
                    isOpponentDropping = false;
                    isMyTurn = true;
                    lblTurn.Text = "Your Turn";

                    // Check computer win/draw
                    if (CheckWin(opponentNumber))
                    {
                        ShowResult("Computer wins!", Color.Red);
                        return;
                    }
                    if (IsBoardFull())
                    {
                        ShowResult("It's a draw!", Color.DarkOrange);
                        return;
                    }
                }
                else
                {
                    opponentDropRow++;
                }
            }
        }



        private void ShowResult(string text, Color color)
        {
            lblResult.Text = text;
            lblResult.ForeColor = color;
            MessageBox.Show(text);
            gameOver = true;
        }

        private async void SendMoveToServer(int row, int col, bool animateOpponent = true)
        {
            var moveData = new { GameId = gameId, PlayerId = playerId, Row = row, Column = col };

            try
            {
                // Send the move to the server
                var response = await httpClient.PostAsJsonAsync("move", moveData);
                //                response.EnsureSuccessStatusCode();
                MessageBox.Show(
                   response.ToString(),
                   "Print Bambi",
                   MessageBoxButtons.OK);

                // Parse the JSON response
                var rawJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                // Extract the opponentMove object
                if (!root.TryGetProperty("opponentMove", out JsonElement oppEl) &&
                    !root.TryGetProperty("OpponentMove", out oppEl))
                {
                    return;
                }
                if (oppEl.ValueKind == JsonValueKind.Null)
                {
                    // No opponent move in this response, e.g. game ended
                    return;
                }


                // Get the row
                if (!oppEl.TryGetProperty("row", out JsonElement rowEl) &&
                    !oppEl.TryGetProperty("Row", out rowEl))
                {
                    return;
                }
                int oppRow = rowEl.GetInt32();

                // Get the column
                if (!oppEl.TryGetProperty("column", out JsonElement colEl) &&
                    !oppEl.TryGetProperty("Column", out colEl))
                {
                    return;
                }
                int oppCol = colEl.GetInt32();

                if (animateOpponent)
                {
                    opponentDropCol = oppCol;
                    opponentDropRow = 0;
                    opponentTargetRow = oppRow;
                    isOpponentDropping = true;
                    dropTimer.Start();
                }
            }
            catch (HttpRequestException)
            {
                MessageBox.Show(
                    $"Cannot contact game server at {httpClient.BaseAddress}",
                    "Connection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Exception in SendMoveToServer: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }




        private void RestoreTimer_Tick(object? sender, EventArgs e)
        {
            var move = restoredMoves[restoreIndex];
            // Compare against numeric playerId string for restoration
            int mover = move.PlayerId == playerId ? myNumber : opponentNumber;
            board[move.Row, move.Column] = mover;
            restoreIndex++;

            Invalidate();
            Update();

            if (restoreIndex >= restoredMoves.Count)
            {
                restoreTimer.Stop();
                int lastPlayer = restoredMoves[^1].PlayerId == playerId ? myNumber : opponentNumber;
                if (CheckWin(lastPlayer)) ShowResult($"Player {lastPlayer} wins (restored)", Color.Blue);
                else if (IsBoardFull()) ShowResult("It's a draw (restored)", Color.DarkOrange);
                return;
            }
            Invalidate();
        }

        private bool IsBoardFull()
        {
            for (int c = 0; c < Columns; c++)
                if (board[0, c] == 0) return false;
            return true;
        }

        private bool CheckWin(int player)
        {
            // Horizontal
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c <= Columns - 4; c++)
                    if (board[r, c] == player && board[r, c + 1] == player && board[r, c + 2] == player && board[r, c + 3] == player)
                        return true;
            // Vertical
            for (int c = 0; c < Columns; c++)
                for (int r = 0; r <= Rows - 4; r++)
                    if (board[r, c] == player && board[r + 1, c] == player && board[r + 2, c] == player && board[r + 3, c] == player)
                        return true;
            // Diagonal down-right
            for (int r = 0; r <= Rows - 4; r++)
                for (int c = 0; c <= Columns - 4; c++)
                    if (board[r, c] == player && board[r + 1, c + 1] == player && board[r + 2, c + 2] == player && board[r + 3, c + 3] == player)
                        return true;
            // Diagonal down-left
            for (int r = 0; r <= Rows - 4; r++)
                for (int c = 3; c < Columns; c++)
                    if (board[r, c] == player && board[r + 1, c - 1] == player && board[r + 2, c - 2] == player && board[r + 3, c - 3] == player)
                        return true;
            return false;
        }
    }
}
