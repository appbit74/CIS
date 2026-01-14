namespace CIS.ViewModels
{
    // นี่คือ "กล่อง" ที่ Controller จะเตรียมให้ View
    public class AttachmentViewModel
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFilePath { get; set; } = string.Empty;
        public long FileSizeInBytes { get; set; }
        public string Extension { get; set; } = string.Empty; // <-- นี่ไง! ตัวแปรที่เราเตรียมไว้!
    }
}