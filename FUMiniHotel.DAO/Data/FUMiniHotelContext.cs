using FUMiniHotel.Areas.Identity.Data;
using FUMiniHotel.DAO;
using FUMiniHotel.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FUMiniHotel.DAO.Data;

public class FUMiniHotelContext : IdentityDbContext<ApplicationUser>
{
    public FUMiniHotelContext(DbContextOptions<FUMiniHotelContext> options)
        : base(options)
    {
    }
    public DbSet<Question> Questions { get; set; }
    public DbSet<BookingReservation> BookingReservations { get; set; }
    public DbSet<RoomInformation> RoomInformation { get; set; }
    public DbSet<RoomType> RoomType { get; set; }
    public DbSet<BookingDetail> BookingDetail { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Question>()
                .HasOne(q => q.AskedBy)
                .WithMany()
                .HasForeignKey(q => q.AskedById)
                .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Question>()
            .HasOne(q => q.AnsweredBy)
            .WithMany()
            .HasForeignKey(q => q.AnsweredById)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<BookingDetail>()
              .HasOne(bd => bd.BookingReservation)
              .WithMany(br => br.BookingDetails)
              .HasForeignKey(bd => bd.BookingReservationID)
              .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BookingDetail>()
            .HasOne(bd => bd.Room)
            .WithMany()
            .HasForeignKey(bd => bd.RoomID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RoomInformation>()
            .HasOne(ri => ri.RoomType)
            .WithMany()
            .HasForeignKey(ri => ri.RoomTypeID)
            .OnDelete(DeleteBehavior.Cascade);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
    }
}
