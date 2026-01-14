using System.ComponentModel.DataAnnotations;

namespace CIS.Models
{
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "กรุณาระบุทะเบียนรถ")]
        [Display(Name = "เลขทะเบียน")]
        [StringLength(20)]
        public string LicensePlate { get; set; } = string.Empty; // เช่น กก-1234

        [Required]
        [Display(Name = "ยี่ห้อ (Brand)")]
        public string Brand { get; set; } = string.Empty; // เช่น Toyota, Honda

        [Required]
        [Display(Name = "รุ่น (Model)")]
        public string Model { get; set; } = string.Empty; // เช่น Commuter, Altis

        [Required]
        [Display(Name = "ประเภทรถ")]
        public VehicleType CarType { get; set; }

        [Display(Name = "จำนวนที่นั่ง")]
        public int SeatCount { get; set; }

        [Display(Name = "สีรถ (ในปฏิทิน)")]
        public string CalendarColor { get; set; } = "#ff9f89"; // Default สีส้มอ่อน

        [Display(Name = "รูปภาพรถ")]
        public string? ImagePath { get; set; } // (เผื่ออนาคต: เก็บ Path รูปถ่ายรถ)

        [Display(Name = "สถานะการใช้งาน")]
        public bool IsActive { get; set; } = true;

        // Relation
        public virtual ICollection<VehicleBooking> Bookings { get; set; } = new List<VehicleBooking>();
    }
}