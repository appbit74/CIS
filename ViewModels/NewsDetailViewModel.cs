using CIS.Models;

namespace CIS.ViewModels
{
    // นี่คือ "กล่อง" หลัก ที่ Controller จะส่งให้ View "Details"
    public class NewsDetailViewModel
    {
        // 1. ข่าว (ตัวแม่)
        public NewsArticle News { get; set; } = null!;

        // 2. รายการไฟล์แนบ (ที่ "ประมวลผลแล้ว")
        public List<AttachmentViewModel> ProcessedAttachments { get; set; }

        public NewsDetailViewModel()
        {
            ProcessedAttachments = new List<AttachmentViewModel>();
        }
    }
}