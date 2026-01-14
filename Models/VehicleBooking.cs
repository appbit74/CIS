using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIS.Models
{
    public class VehicleBooking
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "กรุณาระบุจุดประสงค์/หัวข้อ")]
        [Display(Name = "จุดประสงค์การใช้รถ")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุสถานที่ปลายทาง")]
        [Display(Name = "สถานที่ไป (Destination)")]
        public string Destination { get; set; } = string.Empty;

        [Required]
        [Display(Name = "เวลาเริ่ม")]
        public DateTime StartTime { get; set; }

        [Required]
        [Display(Name = "เวลาสิ้นสุด")]
        public DateTime EndTime { get; set; }

        [Display(Name = "จำนวนผู้โดยสาร")]
        public int PassengerCount { get; set; }

        [Display(Name = "ต้องการคนขับรถ")]
        public bool IsDriverRequired { get; set; } = false; // False = ขับเอง

        [Display(Name = "ผู้จอง")]
        public string BookedBy { get; set; } = string.Empty;

        // (เราใช้ Enum เดิมจากห้องประชุมได้เลย หรือจะสร้างใหม่ก็ได้)
        [Display(Name = "สถานะ")]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        // Relation
        [Required(ErrorMessage = "กรุณาเลือกรถ")]
        public int VehicleId { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle Vehicle { get; set; } = null!;
    }
}