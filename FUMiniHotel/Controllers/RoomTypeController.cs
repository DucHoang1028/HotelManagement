using FUMiniHotel.DAO;
using FUMiniHotel.Repositories.IRepositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FUMiniHotel.Controllers
{ 
   public class RoomTypeController : Controller
{
    private readonly IRoomRepository _roomRepository;
    public RoomTypeController(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }
    public async Task<IActionResult> Index()
    {
        var roomType = await _roomRepository.GetAllRoomTypeAsync();
        return View(roomType);
    }
    public async Task<IActionResult> Detail(int id)
    {
        var roomType = await _roomRepository.GetAllRoomTypeIdAsync(id);
        if (roomType == null)
        {
            return NotFound();
        }
        return View(roomType);
    }
    [HttpGet]
        public async Task<IActionResult> Manage(string searchQuery, string roleFilter, int? page)
        {
            var roomTypes = await _roomRepository.GetAllRoomTypeAsync();

            // Pagination logic
            int pageSize = 5; // Number of room types per page
            int pageNumber = page ?? 1; // Default to page 1 if no page is specified

            // Calculate total room types and paginate the data
            var totalRoomTypes = roomTypes.Count();
            var paginatedRoomTypes = roomTypes.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            // Pass pagination data to the view
            ViewBag.TotalRoomTypes = totalRoomTypes;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRoomTypes / (double)pageSize);

            return View(paginatedRoomTypes);
        }

        public async Task<IActionResult> Create()
    {
        var roomType = await _roomRepository.GetAllRoomTypeAsync();
        return View(new RoomType());
    }
    [HttpPost]
    public async Task<IActionResult> Create(RoomType roomType)
    {
        if (ModelState.IsValid)
        {
            await _roomRepository.AddRoomTypeAsync(roomType);
            return RedirectToAction("Manage");
        }
        return View(roomType);
    }
    public async Task<IActionResult> Edit(int id)
    {
        var roomType = await _roomRepository.GetAllRoomTypeIdAsync(id);
        return View(roomType);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(int id, RoomType roomType)
    {
        if (id != roomType.RoomTypeID)
        {
            return BadRequest();
        }
        await _roomRepository.UpdateRoomTypeAsync(roomType);
        return RedirectToAction("Manage");

    }
    public async Task<IActionResult> Delete(int id)
    {
        var roomType = await _roomRepository.GetAllRoomTypeIdAsync(id);
        if (roomType == null)
        {
            return NotFound();
        }
        return View(roomType);
    }
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _roomRepository.DeleteRoomTypeAsync(id);
        return RedirectToAction("Manage");
    }
}
}