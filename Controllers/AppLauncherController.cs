using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http; // สำหรับยิง API
using CIS.Data;
using CIS.Models;

namespace CIS.Controllers
{
    public class AppLauncherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory; // ใช้สำหรับยิง API ภายนอก

        public AppLauncherController(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // 1. หน้า Dashboard แสดงปุ่มต่างๆ
        public async Task<IActionResult> Dashboard()
        {
            // ดึงข้อมูล Group และ Link ภายใน Group นั้นๆ
            var groups = await _context.LauncherGroups
                .Include(g => g.LauncherLinks)
                .OrderBy(g => g.DisplayOrder)
                .ToListAsync();

            return View(groups);
        }

        // 2. พระเอกของเรา: Action สำหรับกดลิ้งค์
        // ปุ่มทุกปุ่มในหน้า Dashboard จะวิ่งมาที่นี่ก่อน เพื่อตรวจสอบว่าเป็น Link แบบไหน
        public async Task<IActionResult> OpenApp(int id)
        {
            var app = await _context.LauncherLinks.FindAsync(id);
            if (app == null) return NotFound();

            // กรณีที่ 1: ลิ้งค์ธรรมดา -> Redirect ไปเลย
            if (app.Method == LaunchMethod.DirectLink)
            {
                return Redirect(app.TargetUrl);
            }

            // กรณีที่ 2: ต้องยิง API Authen ก่อน (Logic API Post)
            if (app.Method == LaunchMethod.ApiPostAuth)
            {
                // TODO: เขียน Logic การยิง API ของคุณตรงนี้
                // ตัวอย่าง: ยิงไปขอ Token แล้วค่อย Redirect พร้อม Token

                string oneTimeToken = await MockApiAuthentication(app.TargetUrl);

                // สมมติว่าปลายทางต้องการ Token ผ่าน Query String หรือ Header
                // ในที่นี้สมมติว่าแนบไปกับ URL
                string finalUrl = $"{app.TargetUrl}?token={oneTimeToken}";

                return Redirect(finalUrl);
            }

            return Redirect(app.TargetUrl);
        }

        // จำลองฟังก์ชั่นยิง API (คุณต้องแก้ code นี้ให้เป็นของจริง)
        private async Task<string> MockApiAuthentication(string targetUrl)
        {
            // var client = _httpClientFactory.CreateClient();
            // var response = await client.PostAsync(...)
            // return token;

            await Task.Delay(500); // จำลองการโหลด
            return "sample_token_from_api_123456";
        }
    }
}