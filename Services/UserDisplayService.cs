using CIS.Data;
using CIS.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CIS.Services
{
    public class UserDisplayService
    {
        private readonly ApplicationDbContext _context;

        public UserDisplayService(ApplicationDbContext context)
        {
            _context = context;
        }

        public string GetDisplayName(string adUsername)
        {
            if (string.IsNullOrEmpty(adUsername)) return "ผู้เยี่ยมชม";

            // 1. ตัด Domain ทิ้ง (เช่น CRIMCAD\sutthanit -> sutthanit)
            string cleanUsername = adUsername.Contains("\\")
                                   ? adUsername.Split('\\').Last()
                                   : adUsername;

            // 2. ค้นหาใน DB (ใช้ GeneratedUsername เป็น Key)
            // ใช้ AsNoTracking เพื่อความเร็ว เพราะเราแค่อ่านมาโชว์ ไม่ได้แก้ไข
            var employee = _context.EmployeeProfiles
                                   .AsNoTracking()
                                   .FirstOrDefault(u => u.GeneratedUsername == cleanUsername);

            // 3. ถ้าเจอ: ส่งชื่อเต็มกลับไป
            if (employee != null)
            {
                // รวม คำนำหน้า + ชื่อ + นามสกุล
                return $"{employee.Title}{employee.FirstNameTH} {employee.LastNameTH}".Trim();
            }

            // 4. ถ้าไม่เจอ: ส่ง Username เดิมกลับไป (Fallback)
            return cleanUsername; // หรือจะส่ง adUsername ก็ได้
        }

        // (แถม) ฟังก์ชันดึงรูปโปรไฟล์ หรือ ตำแหน่ง ถ้าอนาคตอยากใช้
        public string GetUserPosition(string adUsername)
        {
            // ... logic คล้ายข้างบน ...
            return "";
        }
    }
}