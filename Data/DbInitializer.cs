using CIS.Models;
using System.Linq;

namespace CIS.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // ตรวจสอบว่ามีฐานข้อมูลจริงไหม ถ้าไม่มีให้สร้าง
            context.Database.EnsureCreated();

            // ตรวจสอบว่ามีข้อมูลกลุ่มหรือยัง (ถ้ามีแล้ว ให้จบการทำงาน ไม่ต้องเพิ่มซ้ำ)
            if (context.LauncherGroups.Any())
            {
                return;
            }

            // ถ้ายังไม่มี ให้สร้างข้อมูลกลุ่มตั้งต้น
            var groups = new LauncherGroup[]
            {
                new LauncherGroup { GroupName = "ระบบภายในศาล", DisplayOrder = 1 },
                new LauncherGroup { GroupName = "ระบบส่วนกลางศาลยุติธรรม", DisplayOrder = 2 },
                new LauncherGroup { GroupName = "เว็บไซต์หน่วยงานภายนอก", DisplayOrder = 3 }
            };

            // บันทึกลง Database
            context.LauncherGroups.AddRange(groups);
            context.SaveChanges();
        }
    }
}