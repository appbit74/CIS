using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CIS.Models
{
    // 1. กำหนดประเภทของการเรียกใช้งาน (Link ธรรมดา หรือ ยิง API)
    public enum LaunchMethod
    {
        [Display(Name = "Direct Link (ลิ้งค์ธรรมดา)")]
        DirectLink = 0,

        [Display(Name = "API Post Authentication (ส่งค่าผ่าน API)")]
        ApiPostAuth = 1
    }

    // 2. ตารางกลุ่ม (เช่น ระบบภายใน, ระบบส่วนกลาง)
    public class LauncherGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "ชื่อกลุ่มระบบ")]
        public string GroupName { get; set; } // เช่น "ระบบภายในศาล", "ระบบส่วนกลาง"

        [Display(Name = "ลำดับการแสดงผล")]
        public int DisplayOrder { get; set; } = 0;

        // Relation
        public virtual ICollection<LauncherLink> LauncherLinks { get; set; } = new List<LauncherLink>();
    }

    // 3. ตารางลิ้งค์แอพพลิเคชั่น
    public class LauncherLink
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "ชื่อระบบ")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "URL ปลายทาง")]
        // ใช้อันนี้แทน: อนุญาตทุกอย่าง ขอแค่ขึ้นต้นด้วย http หรือ https
        [RegularExpression(@"^(http|https)://.*$", ErrorMessage = "URL ต้องขึ้นต้นด้วย http:// หรือ https://")]
        public string TargetUrl { get; set; } = string.Empty;

        [Display(Name = "ไอคอน (CSS Class)")]
        public string IconClass { get; set; } // เช่น "fas fa-gavel"

        [Display(Name = "วิธีการเชื่อมต่อ")]
        public LaunchMethod Method { get; set; }

        [Display(Name = "กลุ่มระบบ")]
        public int LauncherGroupId { get; set; }

        [ForeignKey("LauncherGroupId")]
        public virtual LauncherGroup? LauncherGroup { get; set; }
        // เพิ่ม Property นี้ลงไปใน Class LauncherLink ครับ
        [Display(Name = "สีปุ่ม")]
        public string ColorClass { get; set; } // ตั้งค่าเริ่มต้นเป็นสีน้ำเงิน

        // เพิ่ม Property นี้ลงไปครับ
        [Display(Name = "พารามิเตอร์ที่ส่ง (API)")]
        public string? ApiParameters { get; set; } // เก็บเป็น Text ยาวๆ เช่น "EmployeeId,Email,FullName"

        [Display(Name = "API Secret Key (รหัสลับเชื่อมต่อ)")]
        [StringLength(100)]
        public string? ApiToken { get; set; }
    }
}