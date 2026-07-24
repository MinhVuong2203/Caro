using Caro.DTOs;
using Caro.Models;

namespace Caro.Mapper
{
    public static class RoomMapper
    {
        public static RoomResponse ToResponse(Room room)
        {
            return new RoomResponse
            {
                RoomCode = room.RoomCode,
                Player1 = room.Player1,
                Player2 = room.Player2,
                Player1Ready = room.Player1Ready,
                Player2Ready = room.Player2Ready,
                HostConnectionId = room.HostConnectionId,
                BoardSize = room.BoardSize,
                CurrentTurn = room.CurrentTurn,
                IsPlaying = room.IsPlaying,
                Board = ConvertBoard(room.Board),
                WinningCells = room.WinningCells,
                LastMove = room.LastMove,
                Viewers = room.Viewers,
                DrawRequesterConnectionId = room.DrawRequesterConnectionId
            };
        }
        private static string[][] ConvertBoard(char[,] board)
        {
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            var result = new string[rows][];

            for (int i = 0; i < rows; i++)
            {
                result[i] = new string[cols];

                for (int j = 0; j < cols; j++)
                {
                    result[i][j] = board[i, j] == '\0'
                        ? ""
                        : board[i, j].ToString();
                }
            }

            return result;
        }
    }
}
