using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FUMiniHotel.DAO;
using FUMiniHotel.DAO.Data;
using FUMiniHotel.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace FUMiniHotel.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly FUMiniHotelContext _context;

        public RoomRepository(FUMiniHotelContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RoomInformation>> GetAllRoomAsync()
        {
            return await _context.RoomInformation
                .Include(r => r.RoomType)
                .ToListAsync();
        }

        public async Task<RoomInformation> GetAllRoomIdAsync(int id)
        {
            return await _context.RoomInformation
                 .Include(r => r.RoomType)
                 .FirstOrDefaultAsync(r => r.RoomID == id);
        }
        public async Task AddRoomAsync(RoomInformation info)
        {
            await _context.RoomInformation.AddAsync(info);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateRoomAsync(RoomInformation info)
        {
            
            _context.RoomInformation.Update(info);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteRoomAsync(int id)
        {
            var room =  await _context.RoomInformation.FindAsync(id);
            if (room != null)
            {
                _context.RoomInformation.Remove(room);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<IEnumerable<RoomType>> GetAllRoomTypeAsync()
        {
            return await _context.RoomType
                .ToListAsync();
        }

        public async Task<RoomType> GetAllRoomTypeIdAsync(int id)
        {
            return await _context.RoomType
                .FirstOrDefaultAsync(rt => rt.RoomTypeID == id);
        }
        public async Task AddRoomTypeAsync(RoomType type)
        {
            await _context.RoomType.AddAsync(type);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateRoomTypeAsync(RoomType type)
        {
            _context.RoomType.Update(type);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteRoomTypeAsync(int id)
        {
            var roomType = await _context.RoomType.FindAsync(id);
            if (roomType != null)
            {
                _context.RoomType.Remove(roomType);
                await _context.SaveChangesAsync();
            }
        }

    }
}
