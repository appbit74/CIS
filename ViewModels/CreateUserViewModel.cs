using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CIS.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
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

        [Required(ErrorMessage = "กรุณาระบุหมายเลขบัตร/พนักงาน")]
        [Display(Name = "หมายเลขบัตรประชาชน/พนักงาน (EmployeeID)")]
        public string EmployeeID { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาเลือกสังกัด (OU)")]
        [Display(Name = "สังกัด (OU)")]
        public string SelectedOU { get; set; } = string.Empty;

        public List<SelectListItem> OUList { get; set; } = new List<SelectListItem>();

        // --- 1. *** เราลบ Property "Password" ทิ้งไปจากที่นี่! *** ---

    }
}