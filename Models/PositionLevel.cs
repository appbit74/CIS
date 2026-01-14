using System.ComponentModel.DataAnnotations;

namespace CIS.Models
{
    public enum PositionLevel
    {
        [Display(Name = "เจ้าหน้าที่")]
        Staff = 0,

        [Display(Name = "หัวหน้างาน")]
        Supervisor = 1,

        [Display(Name = "หัวหน้าส่วน")]
        SectionHead = 2,

        [Display(Name = "ผู้บริหาร")]
        Management = 3
    }
}