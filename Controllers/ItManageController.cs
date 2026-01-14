using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CIS.Data;
using CIS.Models;
using System.DirectoryServices.AccountManagement; // ใช้ตาม AdminAdController
using System.DirectoryServices; // ใช้สำหรับ DirectoryEntry
using System.Threading.Tasks;
using System.Linq;
using System;

namespace CIS.Controllers
{
    // กำหนดสิทธิ์ตามเดิม
    [Authorize(Roles = @"CRIMCAD\CIS_Admins")]
#pragma warning disable CA1416 // ปิด Warning เรื่อง Windows Platform
    public class ItManageController : Controller
    {
        private readonly ApplicationDbContext _context;

        // --- 1. การตั้งค่า (Configuration) แบบ Hardcode ตาม AdminAdController ---
        private readonly string _domainName = "CRIMCAD";
        private readonly string _ouRootPath = "OU=Crimc Users,DC=CRIMC,DC=INTRA";
        private readonly string _defaultPassword = "P@ssw0rd"; // รหัสผ่านตั้งต้น

        public ItManageController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // หน้า Dashboard รายชื่อรอ Sync
        // ==========================================
        public async Task<IActionResult> Index()
        {
            // 1. รายชื่อรอ Sync (พนักงานใหม่)
            var pendingEmployees = await _context.EmployeeProfiles
                .Where(e => !e.IsSyncedToAd && e.Status == EmployeeStatus.Active)
                .OrderByDescending(e => e.CreatedDate)
                .ToListAsync();

            // 2. [เพิ่มใหม่] รายชื่อรอ Disable (พนักงานลาออก)
            // เงื่อนไข: สถานะเป็น Resigned แต่ IsAdDisabled ยังเป็น false
            var resignedEmployees = await _context.EmployeeProfiles
                .Where(e => e.Status == EmployeeStatus.Resigned && !e.IsAdDisabled)
                .ToListAsync();

            // ส่งทั้ง 2 ลิสต์ไปที่ View (ใช้ ViewModel หรือ ViewBag ก็ได้)
            ViewBag.PendingList = pendingEmployees;
            ViewBag.ResignedList = resignedEmployees;

            return View(); // แก้ Index.cshtml ให้รับข้อมูลแบบใหม่
        }

        // ==========================================
        // Action: กดปุ่มเพื่อสร้าง User (Sync)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncToAd(int id)
        {
            var employee = await _context.EmployeeProfiles.FindAsync(id);
            if (employee == null) return NotFound();

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, _domainName, _ouRootPath))
                {
                    UserPrincipal existingAdUser = null;
                    string matchMethod = "";

                    // -----------------------------------------------------------
                    // STEP 1: ตรวจสอบด้วย "เลขบัตรประชาชน" (แม่นยำที่สุด)
                    // -----------------------------------------------------------
                    if (!string.IsNullOrEmpty(employee.CitizenId))
                    {
                        var userTemplate = new UserPrincipal(context);
                        userTemplate.EmployeeId = employee.CitizenId;
                        using (var searcher = new PrincipalSearcher(userTemplate))
                        {
                            existingAdUser = searcher.FindOne() as UserPrincipal;
                        }

                        if (existingAdUser != null) matchMethod = "เลขบัตรประชาชน";
                    }

                    // -----------------------------------------------------------
                    // STEP 2: ถ้าไม่เจอ ให้ตรวจสอบด้วย "Username" (เผื่อข้อมูลเก่าไม่มีเลขบัตร)
                    // -----------------------------------------------------------
                    if (existingAdUser == null)
                    {
                        existingAdUser = UserPrincipal.FindByIdentity(context, employee.GeneratedUsername);
                        if (existingAdUser != null) matchMethod = "Username";
                    }

                    // =======================================================
                    // กรณี A: เจอ User ในระบบแล้ว (ไม่ว่าจะเจอด้วยวิธีไหน)
                    // =======================================================
                    if (existingAdUser != null)
                    {
                        // ตรวจสอบสถานะ: ถูกระงับสิทธิ์หรือไม่?
                        if (existingAdUser.Enabled == false)
                        {
                            // >>> Condition: ระงับสิทธิ์ -> Auto Delete ใน HR <<<

                            string foundUser = existingAdUser.SamAccountName;

                            // ลบข้อมูลออกจาก HR Database
                            _context.EmployeeProfiles.Remove(employee);
                            await _context.SaveChangesAsync();

                            TempData["ErrorMessage"] = $"⛔ ตรวจสอบพบ User: {foundUser} (ตรงกับ{matchMethod}) ถูกระงับสิทธิ์ใน AD -> ระบบได้ลบใบสมัครนี้อัตโนมัติ";
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            // >>> Condition: ใช้งานได้ปกติ -> Auto Sync <<<

                            // 1. อัปเดตข้อมูลใน HR ให้ตรงกับ AD
                            employee.IsSyncedToAd = true;
                            employee.GeneratedUsername = existingAdUser.SamAccountName; // ปรับชื่อให้ตรง AD เป๊ะๆ
                            if (employee.GetType().GetProperty("SyncDate") != null)
                                employee.SyncDate = DateTime.Now;

                            _context.Update(employee);
                            await _context.SaveChangesAsync();

                            // 2. [Data Enrichment] หากเจอด้วย Username แต่ใน AD ไม่มีเลขบัตร
                            // ให้ถือโอกาสนี้เอาเลขบัตรจาก HR ยัดกลับเข้าไปใน AD ด้วยเลย (ซ่อมข้อมูลเก่า)
                            bool adUpdated = false;
                            using (var de = existingAdUser.GetUnderlyingObject() as DirectoryEntry)
                            {
                                if (de.Properties["employeeID"].Value == null && !string.IsNullOrEmpty(employee.CitizenId))
                                {
                                    de.Properties["employeeID"].Value = employee.CitizenId;
                                    de.CommitChanges();
                                    adUpdated = true;
                                }
                            }

                            string extraMsg = adUpdated ? "และอัปเดตเลขบัตรลง AD ให้แล้ว" : "";
                            TempData["SuccessMessage"] = $"✅ Auto Sync สำเร็จ: พบข้อมูลเดิม (ตรงกับ{matchMethod}) เชื่อมโยงข้อมูล{extraMsg}เรียบร้อย";
                            return RedirectToAction(nameof(Index));
                        }
                    }

                    // =======================================================
                    // กรณี B: ไม่เจอข้อมูลเลย -> สร้างใหม่ (Create New)
                    // =======================================================

                    using (UserPrincipal newUser = new UserPrincipal(context))
                    {
                        newUser.SamAccountName = employee.GeneratedUsername;
                        newUser.UserPrincipalName = $"{employee.GeneratedUsername}@{_domainName.ToLower()}.intra";
                        newUser.DisplayName = $"{employee.FirstNameTH} {employee.LastNameTH}";
                        newUser.GivenName = employee.FirstNameEN;
                        newUser.Surname = employee.LastNameEN;
                        newUser.Description = $"{employee.Position} - {employee.Division}";
                        newUser.EmailAddress = employee.PersonalEmail;
                        newUser.EmployeeId = employee.CitizenId; // ใส่เลขบัตรเสมอ
                        newUser.Enabled = true;
                        newUser.SetPassword(_defaultPassword);
                        newUser.ExpirePasswordNow();

                        newUser.Save();

                        // บันทึก EmployeeID ย้ำอีกรอบผ่าน DirectoryEntry (เพื่อความชัวร์)
                        using (var de = newUser.GetUnderlyingObject() as DirectoryEntry)
                        {
                            if (de != null && !string.IsNullOrEmpty(employee.CitizenId))
                            {
                                de.Properties["employeeID"].Value = employee.CitizenId;
                                de.CommitChanges();
                            }
                        }
                    }

                    // อัปเดต DB
                    employee.IsSyncedToAd = true;
                    if (employee.GetType().GetProperty("SyncDate") != null) employee.SyncDate = DateTime.Now;

                    _context.Update(employee);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"✅ สร้าง User AD ใหม่: {employee.GeneratedUsername} สำเร็จ!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ เกิดข้อผิดพลาด: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Action: กดปุ่มเพื่อระงับ User (Disable)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableAdUser(int id)
        {
            var employee = await _context.EmployeeProfiles.FindAsync(id);
            if (employee == null) return NotFound();

            try
            {
                // ใช้ Direct Logic แบบที่คุณถนัด
                using (var context = new PrincipalContext(ContextType.Domain, _domainName, _ouRootPath))
                {
                    var user = UserPrincipal.FindByIdentity(context, employee.GeneratedUsername);
                    if (user != null)
                    {
                        user.Enabled = false; // *** สั่ง Disable ***
                        user.Description += " [Resigned - Disabled by System]"; // แปะ Note ไว้หน่อย
                        user.Save();
                    }
                    else
                    {
                        // ถ้าหาไม่เจอใน AD (อาจจะลบไปแล้ว) ก็ถือว่าจบงาน
                        TempData["ErrorMessage"] = "ไม่พบ User ใน AD (อาจถูกลบไปแล้ว)";
                    }
                }

                // อัปเดตใน DB ว่าจัดการแล้ว
                employee.IsAdDisabled = true;
                _context.Update(employee);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"ระงับสิทธิ์ (Disable) คุณ {employee.GeneratedUsername} เรียบร้อยแล้ว";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}