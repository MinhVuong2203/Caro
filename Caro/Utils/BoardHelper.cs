using Caro.Models;

namespace Caro.Utils
{
    public static class BoardHelper
    {
        public static List<Position>? CheckWinner(char[,] board, int row, int col)
        {
            char symbol = board[row, col];

            var result = CountDirection(board, row, col, 0, 1, symbol); // ngang
            if (result.Count >= 5) return result;

            result = CountDirection(board, row, col, 1, 0, symbol); // dọc
            if (result.Count >= 5) return result;

            result = CountDirection(board, row, col, 1, 1, symbol); // chéo chính
            if (result.Count >= 5) return result;

            result = CountDirection(board, row, col, 1, -1, symbol); // chéo phụ
            if (result.Count >= 5) return result;

            return null;
        }

        private static List<Position> CountDirection(char[,] board, int row, int col, int dx, int dy, char symbol)
        {
            var positions = new List<Position>();

            // luôn thêm quân vừa đánh
            positions.Add(new Position
            {
                Row = row,
                Col = col
            });

            // chiều thuận
            int r = row + dx;
            int c = col + dy;

            while (IsValid(board, r, c) && board[r, c] == symbol)
            {
                positions.Add(new Position
                {
                    Row = r,
                    Col = c
                });

                r += dx;
                c += dy;
            }

            // chiều ngược
            r = row - dx;
            c = col - dy;

            while (IsValid(board, r, c) && board[r, c] == symbol)
            {
                positions.Insert(0, new Position
                {
                    Row = r,
                    Col = c
                });

                r -= dx;
                c -= dy;
            }

            return positions;
        }

        private static bool IsValid(char[,] board, int row, int col)
        {
            return row >= 0 && row < board.GetLength(0) && col >= 0 && col < board.GetLength(1);
        }

    }
}
