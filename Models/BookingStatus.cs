namespace CIS.Models
{
    public enum BookingStatus
    {
        Pending = 0,   // รออนุมัติ
        Approved = 1,  // อนุมัติแล้ว
        Rejected = 2,  // ไม่อนุมัติ
        Cancelled = 3  // ยกเลิกโดยผู้จอง
    }
}