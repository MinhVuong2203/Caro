using Caro.DTOs;
using Caro.Interfaces;
using Caro.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Caro.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private readonly IRoomManager _roomManager;

        public RoomController(IRoomManager roomManager)
        {
            _roomManager = roomManager;
        }

        [HttpPost]
        public ActionResult CreateRoom(CreateRoomRequest request)
        {
            try
            {
                var room = _roomManager.CreateRoom(
                    request.PlayerName,
                    request.BoardSize,
                    Guid.NewGuid().ToString()
                    );
                return Ok( new { message = "Đã tạo phòng thành công", roomCode = room.RoomCode, player = room.Player1?.Name});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra trong quá trình tạo phòng", error = ex.Message });
            }
        }
    }
}
