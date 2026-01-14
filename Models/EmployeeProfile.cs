using System.ComponentModel.DataAnnotations;

namespace CIS.Models
{
    // Enum สถานะพนักงาน
    public enum EmployeeStatus
    {
        Pending = 0,    // รออนุมัติ
        Active = 1,     // พนักงานปกติ
        Resigned = 9    // ลาออก/พ้นสภาพ (รอ IT Disable)
    }

    public class EmployeeProfile
    {
        [Key]
        public int Id { get; set; }

        // 1. หมายเลขบัตรประชาชน (ใช้เป็น Key อ้างอิงได้)
        [Required(ErrorMessage = "กรุณาระบุเลขบัตรประชาชน")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "เลขบัตรประชาชนต้องมี 13 หลัก")]
        [Display(Name = "เลขบัตรประชาชน")]
        public string CitizenId { get; set; } = string.Empty;

        // 2. คำนำหน้า
        [Required]
        [Display(Name = "คำนำหน้า")]
        public string Title { get; set; } = string.Empty; // นาย, นาง, นางสาว

        // 3-6. ชื่อ-นามสกุล (ไทย/อังกฤษ)
        [Required]
        [Display(Name = "ชื่อ (ไทย)")]
        public string FirstNameTH { get; set; } = string.Empty;

        [Required]
        [Display(Name = "นามสกุล (ไทย)")]
        public string LastNameTH { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name (English)")]
        public string FirstNameEN { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name (English)")]
        public string LastNameEN { get; set; } = string.Empty;

        // 7. วันเกิด
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "วันเดือนปีเกิด")]
        public DateTime DateOfBirth { get; set; }

        // 8. เบอร์โทร
        [Required]
        [Display(Name = "เบอร์โทรศัพท์")]
        public string PhoneNumber { get; set; } = string.Empty;

        // 9. Email
        [Required]
        [EmailAddress]
        [Display(Name = "อีเมลส่วนตัว/สำรอง")]
        public string PersonalEmail { get; set; } = string.Empty;

        // 10. กลุ่มตำแหน่ง (ระดับ)
        [Required]
        [Display(Name = "กลุ่มตำแหน่ง")]
        public PositionLevel PositionLevel { get; set; } = PositionLevel.Staff;

        // 11. ตำแหน่ง (Position)
        [Display(Name = "รหัสตำแหน่ง")]
        public int PositionId { get; set; } // <--- เพิ่มตัวนี้ เพื่อรองรับ Dropdown

        [Required]
        [Display(Name = "ตำแหน่ง")]
        public string Position { get; set; } = string.Empty; // ชื่อตำแหน่ง (เช่น นักวิชาการ...)


        // 12. หน่วยงานหลัก (ส่วน)
        [Display(Name = "รหัสส่วนงาน (Division ID)")]
        public int DivisionId { get; set; }

        [Required]
        [Display(Name = "ส่วน (Division)")]
        public string Division { get; set; } = string.Empty;

        // 13. หน่วยงานย่อย (งาน)
        [Display(Name = "รหัสงาน (Section ID)")]
        public int SectionId { get; set; }

        [Required]
        [Display(Name = "งาน (Section)")]
        public string Section { get; set; } = string.Empty;

        // --- System Fields ---

        [Display(Name = "Username ที่ระบบสร้างให้")]
        public string? GeneratedUsername { get; set; }

        [Display(Name = "สถานะการสร้าง User AD")]
        public bool IsSyncedToAd { get; set; } = false; // True = สร้างใน AD แล้ว

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? SyncDate { get; internal set; }

        // เพิ่ม Status
        public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

        // เพิ่ม Flag ว่า IT กด Disable ใน AD ไปหรือยัง
        public bool IsAdDisabled { get; set; } = false;
    }
}