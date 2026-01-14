using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic; // 1. *** เพิ่ม Using นี้ ***

namespace CIS.ViewModels
{
    public class EditUserViewModel
    {
        [Display(Name = "Username (SamAccountName)")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name (GivenName)")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name (Surname)")]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "เปิดใช้งาน (Enabled)")]
        public bool IsEnabled { get; set; } = true;

        // (ส่วนของ OU ... เหมือนเดิม)
        [Required(ErrorMessage = "กรุณาเลือกสังกัด (OU)")]
        [Display(Name = "สังกัด (OU)")]
        public string SelectedOU { get; set; } = string.Empty;
        public List<SelectListItem> OUList { get; set; } = new List<SelectListItem>();

        // --- 2. *** นี่คือส่วนที่เพิ่มเข้ามา (สำหรับ Security Groups) *** ---
        public List<GroupCheckboxViewModel> GroupMemberships { get; set; }

        // (Constructor)
        public EditUserViewModel()
        {
            GroupMemberships = new List<GroupCheckboxViewModel>();
        }
    }
}