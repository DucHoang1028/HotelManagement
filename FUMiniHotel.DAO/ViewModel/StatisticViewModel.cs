using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FUMiniHotel.DAO.ViewModel
{
    public class StatisticsViewModel
    {
        public int TotalRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public decimal OccupancyRate { get; set; } // Percentage
        public decimal TotalRevenue { get; set; }
    }
}
