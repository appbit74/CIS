using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CIS.Data;
using CIS.Models;
using CIS.Services;
using System.Linq;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;

namespace CIS.Controllers
{
    [Authorize(Roles = @"CRIMCAD\HR_Users,CRIMCAD\CIS_Admins")]
    public class HrController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrimsApiService _crimsService;

        // Config สำหรับต่อ AD
        private readonly string _domainName = "CRIMCAD";
        private readonly string _ouRootPath = "OU=Crimc Users,DC=CRIMC,DC=INTRA";

        public HrController(ApplicationDbContext context, ICrimsApiService crimsService)
        {
            _context = context;
            _crimsService = crimsService;
        }

        // --- หน้า Dashboard ---
        public async Task<IActionResult> Index()
        {
            return View(await _context.EmployeeProfiles
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync());
        }

        // --- หน้ากรอกข้อมูล (Create) ---
        public async Task<IActionResult> Create()
        {
            await PrepareDropdownLists(); // เรียกฟังก์ชันโหลด Dropdown
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeProfile employee)
        {
            // ลบการตรวจสอบ IsSyncedToAd ออกจาก ModelState เพราะเราจะ set เอง
            ModelState.Remove("IsSyncedToAd");
            ModelState.Remove("GeneratedUsername");// ลบ Validation ชื่อออก เพราะเราจะ Gen จาก ID
            ModelState.Remove("Division");
            ModelState.Remove("Section");
            ModelState.Remove("Position"); // เอา Validation ชื่อออก เพราะเราจะ Gen จาก ID

            if (ModelState.IsValid)
            {
                // ---------------------------------------------------------
                // [Logic ใหม่] ค้นหาชื่อจาก ID ที่ส่งมา เพื่อบันทึกทั้งคู่
                // ---------------------------------------------------------

                // 1. หาชื่อ Division
                if (employee.DivisionId > 0)
                {
                    var div = await _context.Divisions.FindAsync(employee.DivisionId);
                    employee.Division = div?.Name ?? ""; // บันทึกชื่อลง DB ด้วย
                }

                // 2. หาชื่อ Section (เรียกผ่าน Service หรือ DB Local)
                if (employee.SectionId > 0)
                {
                    var allSections = await _crimsService.GetSectionsAsync();

                    // แก้ไขการค้นหา:
                    // 1. ใช้ .Key แทน .Id
                    // 2. แปลง SectionId (int) เป็น String เพื่อเทียบกับ Key (string)
                    var sec = allSections.FirstOrDefault(s => s.Key == employee.SectionId.ToString());

                    // แก้ไขการดึงชื่อ:
                    // ใช้ .Value แทน .Name
                    // เช็คว่า Key ไม่เป็น null (เจอข้อมูล) ถึงจะเอา Value มาใส่
                    if (!string.IsNullOrEmpty(sec.Key))
                    {
                        employee.Section = sec.Value;
                    }
                    else
                    {
                        employee.Section = ""; // หรือค่า Default
                    }
                }

                // [เพิ่มใหม่] 3. หาชื่อ Position จาก API
                if (employee.PositionId > 0)
                {
                    var allPositions = await _crimsService.GetPositionsAsync();
                    var pos = allPositions.FirstOrDefault(p => p.Key == employee.PositionId.ToString());

                    if (!string.IsNullOrEmpty(pos.Key))
                    {
                        employee.Position = pos.Value; // บันทึกชื่อตำแหน่งลง DB
                    }
                    else
                    {
                        employee.Position = "ไม่ระบุ";
                    }
                }

                // 1. สร้าง Username จากชื่อภาษาอังกฤษ (FirstNameEN) + เลขบัตร 6 หลักท้าย
                // (ถ้าไม่มีชื่ออังกฤษ ให้ใช้ชื่อไทยไปก่อน หรือจะบังคับให้กรอกก็ได้)
                string nameBase = !string.IsNullOrEmpty(employee.FirstNameEN)
                                  ? employee.FirstNameEN
                                  : "user";

                string cleanName = nameBase.Trim().ToLower().Replace(" ", "");
                string citizenId = (employee.CitizenId ?? "").Trim();
                string last6Digits = citizenId.Length >= 6 ? citizenId.Substring(citizenId.Length - 6) : "000000";

                employee.GeneratedUsername = $"{cleanName}.{last6Digits}";

                // 2. Set ค่าเริ่มต้น
                employee.CreatedDate = DateTime.Now;
                employee.IsSyncedToAd = false;
                employee.Status = EmployeeStatus.Active; // เริ่มต้นให้เป็น Active

                _context.Add(employee);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"บันทึกข้อมูลคุณ {employee.FirstNameTH} เรียบร้อย! (Username: {employee.GeneratedUsername})";
                return RedirectToAction(nameof(Index));
            }

            // ถ้า Validate ไม่ผ่าน ให้โหลด Dropdown กลับไปใหม่
            await PrepareDropdownLists(employee.DivisionId, employee.SectionId, employee.PositionId);
            return View(employee);
        }

        // --- หน้าแก้ไขข้อมูล (Edit) ---
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.EmployeeProfiles.FindAsync(id);
            if (employee == null) return NotFound();

            // โหลด Dropdown พร้อมเลือกค่าเดิม
            await PrepareDropdownLists(employee.DivisionId, employee.SectionId);

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeProfile employee)
        {
            if (id != employee.Id) return NotFound();

            // 1. [แก้ไข] เอา Field ที่เราจะจัดการเอง ออกจาก Validation
            ModelState.Remove("GeneratedUsername");
            ModelState.Remove("IsSyncedToAd");

            // *** สำคัญมาก: เอา field string ออก เพราะหน้าเว็บส่งมาแต่ ID ***
            ModelState.Remove("Division");
            ModelState.Remove("Section");
            ModelState.Remove("Position"); // เอา Validation ชื่อออก เพราะเราจะ Gen จาก ID

            if (ModelState.IsValid)
            {
                try
                {
                    var originalEmp = await _context.EmployeeProfiles.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
                    if (originalEmp == null) return NotFound();

                    // -----------------------------------------------------------
                    // 2. [แก้ไข] Logic แปลง ID กลับเป็น Name (เหมือนตอน Create)
                    // -----------------------------------------------------------

                    // 2.1 หาชื่อ Division
                    if (employee.DivisionId > 0)
                    {
                        var div = await _context.Divisions.FindAsync(employee.DivisionId);
                        employee.Division = div?.Name ?? originalEmp.Division; // ถ้าหาไม่เจอให้ใช้ค่าเดิม
                    }
                    else
                    {
                        employee.Division = originalEmp.Division; // ถ้าไม่ได้ส่ง ID มา ให้ใช้ค่าเดิม
                    }

                    // 2.2 หาชื่อ Section (จาก Service)
                    if (employee.SectionId > 0)
                    {
                        var allSections = await _crimsService.GetSectionsAsync();
                        // ใช้ .Key เพราะ Dropdown เราส่ง Key มาเป็น ID
                        var sec = allSections.FirstOrDefault(s => s.Key == employee.SectionId.ToString());
                        if (!string.IsNullOrEmpty(sec.Key))
                        {
                            employee.Section = sec.Value; // บันทึกชื่อลง DB
                        }
                    }
                    else
                    {
                        employee.Section = originalEmp.Section; // ใช้ค่าเดิม
                    }

                    // 2.3 Position (ถ้ายังเป็น Textbox อยู่ ก็จะใช้ค่าจาก employee.Position ได้เลย)
                    // แต่ถ้าเปลี่ยนเป็น Dropdown ID เมื่อไหร่ ต้องเขียน Logic Map ตรงนี้เพิ่มครับ

                    // -----------------------------------------------------------

                    // Logic เดิม: เช็ค AD Sync
                    if (originalEmp.IsSyncedToAd)
                    {
                        employee.GeneratedUsername = originalEmp.GeneratedUsername;
                        employee.CitizenId = originalEmp.CitizenId;
                        employee.IsSyncedToAd = true;
                    }
                    else
                    {
                        // คำนวณ Username ใหม่
                        string nameBase = !string.IsNullOrEmpty(employee.FirstNameEN) ? employee.FirstNameEN : "user";
                        string cleanName = nameBase.Trim().ToLower().Replace(" ", "");
                        string citizenId = (employee.CitizenId ?? "").Trim();
                        string last6Digits = citizenId.Length >= 6 ? citizenId.Substring(citizenId.Length - 6) : "000000";

                        employee.GeneratedUsername = $"{cleanName}.{last6Digits}";
                        employee.IsSyncedToAd = false;
                    }

                    // รักษาค่าเดิม
                    employee.CreatedDate = originalEmp.CreatedDate;
                    employee.SyncDate = originalEmp.SyncDate;

                    // สำคัญ: ห้ามแก้สถานะจากหน้า Edit ทั่วไป (ต้องไปกดปุ่ม Delete/Resign แยก)
                    // หรือถ้าอนุญาตให้แก้ Status ในหน้านี้ได้ ก็คอมเมนต์บรรทัดนี้ออก
                    // employee.Status = originalEmp.Status; 

                    _context.Update(employee);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "บันทึกการแก้ไขเรียบร้อย";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.EmployeeProfiles.Any(e => e.Id == employee.Id)) return NotFound();
                    else throw;
                }
            }

            // ถ้า Error (เช่น ลืมกรอกชื่อ) ให้โหลด Dropdown กลับไปใหม่
            await PrepareDropdownLists(employee.DivisionId, employee.SectionId);
            return View(employee);
        }

        // ==========================================
        // Helper Method: สำหรับโหลด Dropdown List
        // ==========================================
        // ใน Controllers/HrController.cs

        private async Task PrepareDropdownLists(int? selectedDivisionId = null, int? selectedSectionId = null, int? selectedPositionId = null)
        {
            // 1. Division: ส่ง ID เป็น Value, ส่ง Name เป็น Text
            ViewData["DivisionList"] = new SelectList(_context.Divisions, "Id", "Name", selectedDivisionId);

            // 2. Section: (สมมติว่า Service คืนค่ามามี Id กับ Name)
            var sections = await _crimsService.GetSectionsAsync();
            // *** สำคัญ: ต้องระบุ Field ให้ถูก "Id" คือค่าที่ส่งกลับ, "Name" คือคำที่โชว์ ***
            ViewData["SectionList"] = new SelectList(sections, "Key", "Value", selectedSectionId);

            // 3. [เพิ่มใหม่] Position (จาก API)
            var positions = await _crimsService.GetPositionsAsync();
            ViewData["PositionList"] = new SelectList(positions, "Key", "Value", selectedPositionId);

        }

        // ==========================================
        // ส่วนลบพนักงาน (Soft Delete / Resign)
        // ==========================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var employee = await _context.EmployeeProfiles.FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.EmployeeProfiles.FindAsync(id);
            if (employee != null)
            {
                // ปรับสถานะเป็น "ลาออก"
                employee.Status = EmployeeStatus.Resigned;
                employee.IsAdDisabled = false; // แจ้ง IT
                _context.Update(employee);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"แจ้งพ้นสภาพคุณ {employee.FirstNameTH} แล้ว";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // Check AD Status (เหมือนเดิม)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckAdStatus(int id)
        {
            var employee = await _context.EmployeeProfiles.FindAsync(id);
            if (employee == null) return NotFound();

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName, _ouRootPath))
                {
                    UserPrincipal existingAdUser = null;
                    string foundBy = "";

                    // 1. หาจาก EmployeeID (CitizenId)
                    if (!string.IsNullOrEmpty(employee.CitizenId))
                    {
                        var userTemplate = new UserPrincipal(context) { EmployeeId = employee.CitizenId };
                        using (var searcher = new PrincipalSearcher(userTemplate))
                        {
                            existingAdUser = searcher.FindOne() as UserPrincipal;
                            if (existingAdUser != null) foundBy = "เลขบัตรประชาชน";
                        }
                    }

                    // 2. หาจาก Username
                    if (existingAdUser == null)
                    {
                        existingAdUser = UserPrincipal.FindByIdentity(context, employee.GeneratedUsername);
                        if (existingAdUser != null) foundBy = "Username";
                    }

                    if (existingAdUser != null)
                    {
                        employee.IsSyncedToAd = true;
                        employee.GeneratedUsername = existingAdUser.SamAccountName;
                        if (employee.SyncDate == null) employee.SyncDate = DateTime.Now;

                        TempData["SuccessMessage"] = $"✅ พบ User ใน AD ({foundBy}) เชื่อมต่อสำเร็จ";
                    }
                    else
                    {
                        if (employee.IsSyncedToAd)
                        {
                            employee.IsSyncedToAd = false;
                            employee.SyncDate = null;
                            TempData["ErrorMessage"] = "⚠️ ไม่พบ User ใน AD (อาจถูกลบไปแล้ว) -> Reset สถานะ";
                        }
                        else
                        {
                            TempData["InfoMessage"] = "ℹ️ ยังไม่พบ User ในระบบ AD";
                        }
                    }

                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"AD Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}