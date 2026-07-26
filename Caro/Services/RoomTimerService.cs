
using Caro.Hubs;
using Caro.Interfaces;
using Caro.Mapper;
using Caro.Models;
using Microsoft.AspNetCore.SignalR;

namespace Caro.Services
{
    public class RoomTimerService : BackgroundService
    {
        private readonly IRoomManager _roomManager;
        private readonly IHubContext<GameHub> _hubContext;

        public RoomTimerService(IRoomManager roomManager, IHubContext<GameHub> hubContext)
        {
            _roomManager = roomManager;
            _hubContext = hubContext;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var room in _roomManager.GetRooms())
                {
                    if (!room.IsPlaying)
                        continue;

                    if (room.TurnTimeLimit == 0)
                        continue;

                    if (room.TurnDeadline == null)
                        continue;

                    if (DateTime.UtcNow < room.TurnDeadline)
                        continue;

                    Player loser = room.CurrentTurn == 'X' ? room.Player1! : room.Player2!;
                    _roomManager.HandleTurnTimeout(room.RoomCode);

                    await _hubContext.Clients
                        .Group(room.RoomCode)
                        .SendAsync("TimeOut", loser.Name);

                    await _hubContext.Clients
                        .Group(room.RoomCode)
                        .SendAsync("RoomUpdated", RoomMapper.ToResponse(room));
                }

                await Task.Delay(200, stoppingToken);
            }
        }
    }
}
