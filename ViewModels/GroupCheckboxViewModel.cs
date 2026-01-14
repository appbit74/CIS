namespace CIS.ViewModels
{
    // นี่คือ "กล่อง" สำหรับ Checkbox แต่ละอัน
    public class GroupCheckboxViewModel
    {
        // เราจะเก็บ SamAccountName (ชื่อ ID) ของ Group
        public string SamAccountName { get; set; } = string.Empty;

        // นี่คือชื่อที่โชว์ (Name)
        public string DisplayName { get; set; } = string.Empty;

        // นี่คือ "ติ๊ก" (true/false)
        public bool IsSelected { get; set; }
    }
}