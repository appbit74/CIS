using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIS.Models
{
    public class RoomBooking
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "กรุณาระบุหัวข้อการประชุม")]
        [Display(Name = "หัวข้อการประชุม")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "เวลาเริ่ม")]
        public DateTime StartTime { get; set; }

        [Required]
        [Display(Name = "เวลาสิ้นสุด")]
        public DateTime EndTime { get; set; }

        [Display(Name = "ผู้จอง")]
        public string BookedBy { get; set; } = string.Empty; // เก็บ Username

        [Display(Name = "สถานะ")]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        // Relation: การจองนี้ เป็นของห้องไหน
        [Required(ErrorMessage = "กรุณาเลือกห้องประชุม")]
        public int MeetingRoomId { get; set; }

        [ForeignKey("MeetingRoomId")]
        public virtual MeetingRoom MeetingRoom { get; set; } = null!;
    }
}