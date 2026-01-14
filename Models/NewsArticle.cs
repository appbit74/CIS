using System.ComponentModel.DataAnnotations;

namespace CIS.Models
{
    public class NewsArticle
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "กรุณาใส่หัวข้อข่าว")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Html)]
        public string Content { get; set; } = string.Empty;

        public DateTime PublishedDate { get; set; }

        public string Author { get; set; } = string.Empty;

        public bool IsPublished { get; set; } = false;

        // --- 1. *** นี่คือส่วนที่เพิ่มเข้ามา *** ---
        // (บอก EF Core ว่า 1 ข่าว มีได้ "หลาย" (Collection) ไฟล์แนบ)
        public virtual ICollection<NewsAttachment> Attachments { get; set; }

        // 2. (สร้าง Constructor เพื่อไม่ให้ Attachments เป็น null)
        public NewsArticle()
        {
            Attachments = new HashSet<NewsAttachment>();
        }
    }
}