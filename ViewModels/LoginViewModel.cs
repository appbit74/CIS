using System.ComponentModel.DataAnnotations;

namespace CIS.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "กรุณาระบุ Username")]
        [Display(Name = "Username (เช่น username.123456)")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุรหัสผ่าน")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string ReturnUrl { get; set; } = string.Empty;

        // 1. *** "ธง" นี้สำคัญมาก ***
        public bool IsWindowsUserPreFilled { get; set; } = false;
    }
}