using System.ComponentModel.DataAnnotations;

namespace CIS.ViewModels
{
    public class ManageGroupViewModel
    {
        public string GroupName { get; set; } = string.Empty; // (SamAccountName)
        public string Description { get; set; } = string.Empty;

        // รายชื่อสมาชิกปัจจุบัน
        public List<AdUserViewModel> CurrentMembers { get; set; } = new List<AdUserViewModel>();

        // ช่องสำหรับกรอก User ที่จะเพิ่ม
        [Display(Name = "Username ที่ต้องการเพิ่ม (เช่น user.a)")]
        public string NewUsernameToAdd { get; set; } = string.Empty;
    }
}