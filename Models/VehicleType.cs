using System.ComponentModel.DataAnnotations;

namespace CIS.Models
{
    public enum VehicleType
    {
        [Display(Name = "รถเก๋ง (Sedan)")]
        Sedan = 1,

        [Display(Name = "รถตู้ (Van)")]
        Van = 2,

        [Display(Name = "รถกระบะ (Pickup)")]
        Pickup = 3,

        [Display(Name = "รถมอเตอร์ไซค์ (Motorcycle)")]
        Motorcycle = 4,

        [Display(Name = "อื่นๆ")]
        Other = 99
    }
}