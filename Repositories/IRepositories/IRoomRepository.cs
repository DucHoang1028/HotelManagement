using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FUMiniHotel.DAO;

namespace FUMiniHotel.Repositories.IRepositories
{
    public interface IRoomRepository
    {
        //room info
        Task<IEnumerable<RoomInformation>> GetAllRoomAsync();
        Task<RoomInformation> GetAllRoomIdAsync(int id);
        Task AddRoomAsync(RoomInformation info);
        Task UpdateRoomAsync(RoomInformation info);
        Task DeleteRoomAsync(int id);
        //room type
        Task<IEnumerable<RoomType>> GetAllRoomTypeAsync();
        Task<RoomType> GetAllRoomTypeIdAsync(int id);
        Task AddRoomTypeAsync(RoomType type);
        Task UpdateRoomTypeAsync(RoomType type);
        Task DeleteRoomTypeAsync(int id);
    }
}
