using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using CIS.Data;
using CIS.Models;

namespace CIS.Controllers
{
    public class LauncherAdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LauncherAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. หน้าแสดงรายการทั้งหมด
        public async Task<IActionResult> Index()
        {
            var links = await _context.LauncherLinks
                .Include(l => l.LauncherGroup)
                .OrderBy(l => l.LauncherGroup.DisplayOrder)
                .ThenBy(l => l.Title)
                .ToListAsync();
            return View(links);
        }

        // 2. หน้าสร้างใหม่ (GET)
        public IActionResult Create()
        {
            ViewData["LauncherGroupId"] = new SelectList(_context.LauncherGroups, "Id", "GroupName");
            return View();
        }

        // 3. บันทึกการสร้าง (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,TargetUrl,IconClass,Method,LauncherGroupId,ColorClass,ApiParameters,ApiToken")] LauncherLink launcherLink, string[] selectedParams)
        {
            // เพิ่มบรรทัดนี้เพื่อดู Error ใน Console (เฉพาะตอน Debug)
            ModelState.Remove("LauncherGroup");
            if (ModelState.IsValid)
            {
                // แปลง Array จาก Checkbox ให้เป็น String คั่นด้วยเครื่องหมายลูกน้ำ (,)
                if (selectedParams != null && selectedParams.Length > 0)
                {
                    // แปลงจาก Array ["EmployeeId", "Email"] ให้กลายเป็น String "EmployeeId,Email"
                    launcherLink.ApiParameters = string.Join(",", selectedParams);
                }
                else
                {
                    // ถ้าไม่เลือกอะไรเลย หรือเลือกวิธีอื่นที่ไม่ใช่ API ให้เคลียร์ค่าทิ้ง
                    launcherLink.ApiParameters = null;
                }

                _context.Add(launcherLink);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["LauncherGroupId"] = new SelectList(_context.LauncherGroups, "Id", "GroupName", launcherLink.LauncherGroupId);
            return View(launcherLink);
        }

        // 4. หน้าแก้ไข (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var launcherLink = await _context.LauncherLinks.FindAsync(id);
            if (launcherLink == null) return NotFound();

            ViewData["LauncherGroupId"] = new SelectList(_context.LauncherGroups, "Id", "GroupName", launcherLink.LauncherGroupId);
            return View(launcherLink);
        }

        // 5. บันทึกการแก้ไข (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,TargetUrl,IconClass,Method,LauncherGroupId,ColorClass,ApiParameters,ApiToken")] LauncherLink launcherLink, string[] selectedParams)
        {
            if (id != launcherLink.Id) return NotFound();

            ModelState.Remove("LauncherGroup");

            if (ModelState.IsValid)
            {
                try
                {
                    // 2. >>> Logic การบันทึก Parameter (เหมือน Create เป๊ะๆ) <<<
                    if (selectedParams != null && selectedParams.Length > 0)
                    {
                        launcherLink.ApiParameters = string.Join(",", selectedParams);
                    }
                    else
                    {
                        launcherLink.ApiParameters = null;
                    }

                    _context.Update(launcherLink);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LauncherLinkExists(launcherLink.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["LauncherGroupId"] = new SelectList(_context.LauncherGroups, "Id", "GroupName", launcherLink.LauncherGroupId);
            return View(launcherLink);
        }

        // 6. หน้าลบ (GET)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var launcherLink = await _context.LauncherLinks
                .Include(l => l.LauncherGroup)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (launcherLink == null) return NotFound();

            return View(launcherLink);
        }

        // 7. ยืนยันการลบ (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var launcherLink = await _context.LauncherLinks.FindAsync(id);
            if (launcherLink != null)
            {
                _context.LauncherLinks.Remove(launcherLink);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool LauncherLinkExists(int id)
        {
            return _context.LauncherLinks.Any(e => e.Id == id);
        }
    }
}