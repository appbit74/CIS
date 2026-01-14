using System.Collections.Generic;

namespace CIS.Models
{
    public static class HrParameterHelper
    {
        // รายชื่อข้อมูล HR ที่เราอนุญาตให้ส่งออกไปได้
        // Key = ชื่อที่จะเก็บใน DB, Value = ข้อความแสดงให้ Admin เห็น
        public static Dictionary<string, string> AvailableParameters = new Dictionary<string, string>
        {
            { "Username", "ชื่อผู้ใช้ (Username)" },
            { "EmployeeId", "รหัสพนักงาน" },
            { "FullName", "ชื่อ-นามสกุล (ไทย)" },
            { "Email", "อีเมลองค์กร" },
            { "Position", "ตำแหน่งงาน" },
            { "Department", "สังกัด/แผนก" },
            { "ClientIp", "IP Address ของเครื่องที่กด" } // แถมอันนี้ให้ เผื่อปลายทางอยากเก็บ Log
        };
    }
}