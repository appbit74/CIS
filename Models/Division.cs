using System.ComponentModel.DataAnnotations;

namespace CIS.Models
{
    public class Division
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "กรุณาระบุชื่อส่วนงาน")]
        [Display(Name = "ชื่อส่วนงาน")]
        public string Name { get; set; }
    }
}