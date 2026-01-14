using CIS.Data;
using CIS.Models;
using CIS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CIS.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        // Action Index (Dashboard)
        public async Task<IActionResult> Index()
        {
            ViewData["AuthenticatedUser"] = User.Identity?.Name;

            // 1. ดึงข้อมูลข่าว
            var latestNews = await _context.NewsArticles
                .Where(n => n.IsPublished)
                .OrderByDescending(n => n.PublishedDate)
                .Take(5)
                .ToListAsync();

            // 2. ดึงข้อมูล App Launcher และ Group
            var appGroups = await _context.LauncherGroups
                .Include(g => g.LauncherLinks)
                .OrderBy(g => g.DisplayOrder)
                .ToListAsync();

            // 3. สร้าง ViewModel
            var viewModel = new HomeDashboardViewModel
            {
                NewsArticles = latestNews,
                LauncherGroups = appGroups
            };

            return View(viewModel);
        }

        /// Action สำหรับกดลิ้งค์ App
        public async Task<IActionResult> OpenApp(int id)
        {
            var app = await _context.LauncherLinks.FindAsync(id);
            if (app == null) return NotFound();

            if (app.Method == LaunchMethod.DirectLink)
            {
                return Redirect(app.TargetUrl);
            }

            if (app.Method == LaunchMethod.ApiPostAuth)
            {
                var payload = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(app.ApiParameters))
                {
                    var selectedKeys = app.ApiParameters.Split(',');

                    // ========================================================
                    // 1. เตรียมข้อมูล User (ดึงจาก DB Local)
                    // ========================================================
                    string rawUsername = User.Identity?.Name ?? "";

                    // ตัด Domain (CRIMCAD\user -> user)
                    string cleanUsername = rawUsername.Contains("\\")
                                           ? rawUsername.Split('\\').Last()
                                           : rawUsername;

                    // ค้นหาใน DB (รองรับทั้งแบบมีและไม่มี Domain)
                    var currentUser = await _context.EmployeeProfiles
                                            .AsNoTracking() // เพิ่ม AsNoTracking เพื่อประสิทธิภาพ
                                            .FirstOrDefaultAsync(u => (u.GeneratedUsername == cleanUsername || u.GeneratedUsername == rawUsername)
                                                                   && (u.Status == EmployeeStatus.Active || u.Status == EmployeeStatus.Pending));

                    // ========================================================
                    // 2. วนลูปสร้าง Payload ตามที่ Admin เลือกไว้
                    // ========================================================
                    foreach (var key in selectedKeys)
                    {
                        switch (key)
                        {
                            case "Username":
                                payload.Add("username", cleanUsername);
                                break;

                            case "Email":
                                payload.Add("email", currentUser?.PersonalEmail ?? "");
                                break;

                            case "EmployeeId":
                                payload.Add("citizen_id", currentUser?.CitizenId ?? "");
                                payload.Add("emp_db_id", currentUser?.Id.ToString() ?? "0");
                                break;

                            case "FullName":
                                if (currentUser != null)
                                {
                                    // ชื่อไทยเต็ม
                                    payload.Add("fullname_th", $"{currentUser.Title}{currentUser.FirstNameTH} {currentUser.LastNameTH}".Trim());
                                    // แยกชื่อ-นามสกุลไทย
                                    payload.Add("first_name", $"{currentUser.Title}{currentUser.FirstNameTH}");
                                    payload.Add("last_name", $"{currentUser.LastNameTH}");
                                    // ชื่ออังกฤษ
                                    payload.Add("fullname_en", $"{currentUser.FirstNameEN} {currentUser.LastNameEN}");
                                }
                                else
                                {
                                    // ถ้าไม่เจอ User ใน DB ให้ส่ง Username ไปแทน (Fallback)
                                    payload.Add("fullname_th", cleanUsername);
                                    payload.Add("first_name", cleanUsername);
                                    payload.Add("last_name", "");
                                    payload.Add("fullname_en", "");
                                }
                                break;

                            case "Position": // *** จุดที่ปรับปรุง ***
                                payload.Add("position", currentUser?.Position ?? "");
                                // ส่ง Position ID ไปด้วย (ถ้ามี)
                                payload.Add("position_id", currentUser?.PositionId.ToString() ?? "0");
                                break;

                            case "Department":
                                payload.Add("division_id", currentUser?.DivisionId.ToString() ?? "0");
                                payload.Add("division", currentUser?.Division ?? "");
                                payload.Add("section_id", currentUser?.SectionId.ToString() ?? "0");
                                payload.Add("section", currentUser?.Section ?? "");
                                break;

                            case "PhoneNumber": // (แก้จาก Mobile เป็น PhoneNumber ให้ตรงกับ Model)
                                payload.Add("phone_number", currentUser?.PhoneNumber ?? "");
                                break;

                            case "ClientIp":
                                payload.Add("client_ip", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
                                break;
                        }
                    }

                    // =========================================================
                    // 3. ส่ง Token (Secret Key)
                    // =========================================================
                    if (!string.IsNullOrEmpty(app.ApiToken))
                    {
                        payload.Add("api_token", app.ApiToken);
                    }
                    else
                    {
                        payload.Add("api_token", "");
                    }
                }

                // ========================================================
                // 4. ส่งข้อมูลไปหน้า View (Auto Submit Form)
                // ========================================================
                var viewModel = new SsoPostViewModel
                {
                    TargetUrl = app.TargetUrl,
                    Payload = payload
                };

                return View("SsoPost", viewModel);
            }

            return Redirect(app.TargetUrl);
        }

        // Action Privacy และ Error (คงเดิม)
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}