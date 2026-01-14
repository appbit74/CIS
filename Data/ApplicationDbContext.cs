using CIS.Models;
using Microsoft.EntityFrameworkCore;

namespace CIS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<NewsArticle> NewsArticles { get; set; }
        public DbSet<NewsAttachment> NewsAttachments { get; set; }

        // --- ส่วนที่เพิ่มใหม่ (Module 2) ---
        public DbSet<MeetingRoom> MeetingRooms { get; set; }
        public DbSet<RoomBooking> RoomBookings { get; set; }

        // --- ส่วนที่เพิ่มใหม่ (Module 3: Car) ---
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleBooking> VehicleBookings { get; set; }

        public DbSet<EmployeeProfile> EmployeeProfiles { get; set; }

        // --- เพิ่ม DbSet สำหรับ Division ---
        public DbSet<Division> Divisions { get; set; }
        public DbSet<LauncherGroup> LauncherGroups { get; set; }
        public DbSet<LauncherLink> LauncherLinks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 2. ใส่ข้อมูลเริ่มต้น (Seed Data)
            modelBuilder.Entity<Division>().HasData(
                new Division { Id = 1, Name = "ส่วนช่วยอำนวยการ" },
                new Division { Id = 2, Name = "ส่วนคลัง" },
                new Division { Id = 3, Name = "ส่วนจัดการงานคดี" },
                new Division { Id = 4, Name = "ส่วนช่วยพิจารณาคดี" },
                new Division { Id = 5, Name = "ส่วนบริการประชาชนและประชาสัมพันธ์" },
                new Division { Id = 6, Name = "ส่วนไกล่เกลี่ยและประนอมข้อพิพาทฯ" },
                new Division { Id = 7, Name = "ส่วนเทคโนโลยีสารสนเทศ" },
                new Division { Id = 8, Name = "ส่วนบังคับคดีผู้ประกัน" },
                new Division { Id = 9, Name = "ส่วนเจ้าพนักงานตำรวจศาล" },
                new Division { Id = 10, Name = "ผู้บริหาร" }
            );

        }

        public DbSet<SupplyItem> SupplyItems { get; set; }
        public DbSet<SupplyOrder> SupplyOrders { get; set; }
        public DbSet<SupplyOrderItem> SupplyOrderItems { get; set; }

    }
}