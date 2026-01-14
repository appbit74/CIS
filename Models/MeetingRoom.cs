using System.ComponentModel.DataAnnotations;

namespace CIS.Models
{
    public class MeetingRoom
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "กรุณาระบุชื่อห้อง")]
        [Display(Name = "ชื่อห้องประชุม")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "สถานที่/ชั้น")]
        public string Location { get; set; } = string.Empty;

        [Required]
        [Display(Name = "ความจุ (คน)")]
        public int Capacity { get; set; }

        [Display(Name = "สีในปฏิทิน")]
        public string Color { get; set; } = "#3788d8"; // Default สีฟ้า

        public bool IsActive { get; set; } = true; // เปิด/ปิดใช้งานห้อง

        // Relation: 1 ห้อง มีการจองได้หลายรายการ
        public virtual ICollection<RoomBooking> Bookings { get; set; } = new List<RoomBooking>();
    }
}