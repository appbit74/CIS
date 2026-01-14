using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CIS.Models
{
    public class NewsAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OriginalFileName { get; set; } = string.Empty; // ชื่อไฟล์เดิม (e.g., "report.pdf")

        [Required]
        public string StoredFilePath { get; set; } = string.Empty; // Path ที่เก็บจริง (e.g., "/uploads/attachments/guid.pdf")

        public string FileType { get; set; } = string.Empty; // (e.g., "application/pdf")

        public long FileSizeInBytes { get; set; }

        // --- 1. *** นี่คือ Foreign Key (กุญแจนอก) *** ---
        // (เพื่อบอกว่าไฟล์นี้ "เป็นของ" ข่าวชิ้นไหน)
        public int NewsArticleId { get; set; }

        // --- 2. *** นี่คือ Navigation Property (ทางลัด) *** ---
        [ForeignKey("NewsArticleId")]
        public virtual NewsArticle NewsArticle { get; set; } = null!;
    }
}