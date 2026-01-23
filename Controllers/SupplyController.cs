using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CIS.Data;
using CIS.Models;
using Microsoft.AspNetCore.Authorization;
using CIS.Services;
using CIS.ViewModels;

namespace CIS.Controllers
{
    [Authorize]
    public class SupplyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAdService _adService;

        public SupplyController(ApplicationDbContext context, IAdService adService)
        {
            _context = context;
            _adService = adService;
        }

        // ==========================================
        // HELPER METHOD: ดึงข้อมูลพนักงาน (ปรับปรุงใหม่: ตัด Domain ออก)
        // ==========================================
        private async Task<EmployeeProfile> GetCurrentEmployeeAsync()
        {
            // 1. รับค่าที่ Login เข้ามา (อาจจะเป็น "CRIMCAD\user01")
            string fullUsername = User.Identity.Name ?? "";
            string usernameOnly = fullUsername;

            // 2. Logic ตัด Domain: ถ้ามี "\" ให้เอาเฉพาะส่วนหลัง
            if (!string.IsNullOrEmpty(fullUsername) && fullUsername.Contains("\\"))
            {
                var parts = fullUsername.Split('\\');
                if (parts.Length > 1)
                {
                    usernameOnly = parts[1]; // ได้ค่า "user01"
                }
            }

            // 3. ใช้ usernameOnly (ที่ตัดแล้ว) ในการค้นหาใน DB
            var employee = await _context.EmployeeProfiles
                .FirstOrDefaultAsync(e => e.GeneratedUsername == usernameOnly);

            // 4. ถ้าไม่เจอ -> Auto-Sync
            if (employee == null)
            {
                try
                {
                    // เรียก Service ถาม AD (ส่ง usernameOnly ไปถาม)
                    string citizenIdFromAd = _adService.GetCitizenIdByUsername(usernameOnly);

                    if (!string.IsNullOrEmpty(citizenIdFromAd))
                    {
                        // เอาเลขบัตรมาหาใน DB เรา
                        employee = await _context.EmployeeProfiles
                            .FirstOrDefaultAsync(e => e.CitizenId == citizenIdFromAd);

                        // 5. ถ้าเจอตัวจริงใน DB โดยใช้เลขบัตร -> ทำการ Link Username ให้ตรง Format
                        if (employee != null)
                        {
                            // *** บันทึกเฉพาะ usernameOnly ตามโครงสร้าง DB ที่กำหนดไว้ ***
                            employee.GeneratedUsername = usernameOnly;
                            employee.IsSyncedToAd = true;
                            employee.SyncDate = DateTime.Now;

                            _context.Update(employee);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception)
                {
                    // กรณีต่อ AD ไม่ได้ หรือมี Error อื่นๆ ให้ข้ามไป (employee เป็น null ต่อไป)
                }
            }

            return employee;
        }

        // ==========================================
        // 1. ส่วนสำหรับ User (เบิกของ)
        // ==========================================

        public async Task<IActionResult> Index()
        {
            var items = await _context.SupplyItems
                .Where(i => i.IsActive)
                .OrderBy(i => i.ItemName)
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequestViewModel model)
        {
            if (model == null || model.Items == null || !model.Items.Any())
            {
                return Json(new { success = false, message = "ไม่พบรายการวัสดุที่เลือก" });
            }

            // เรียกใช้ Helper ที่ปรับปรุงแล้ว
            var employee = await GetCurrentEmployeeAsync();

            if (employee == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลพนักงานของคุณในระบบ HR (กรุณาติดต่อเจ้าหน้าที่เพื่อตรวจสอบข้อมูล)" });
            }

            var order = new SupplyOrder
            {
                EmployeeId = employee.Id,
                OrderDate = DateTime.Now,
                Status = SupplyStatus.Pending,
                UserRemark = model.Remark,
                OrderItems = new List<SupplyOrderItem>()
            };

            foreach (var reqItem in model.Items)
            {
                var stockItem = await _context.SupplyItems.FindAsync(reqItem.ItemId);
                if (stockItem != null)
                {
                    order.OrderItems.Add(new SupplyOrderItem
                    {
                        SupplyItemId = reqItem.ItemId,
                        Quantity = reqItem.Quantity
                    });
                }
            }

            try
            {
                _context.SupplyOrders.Add(order);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "ส่งใบเบิกเรียบร้อยแล้ว!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "เกิดข้อผิดพลาด: " + ex.Message });
            }
        }

        public async Task<IActionResult> MyHistory()
        {
            var employee = await GetCurrentEmployeeAsync();

            if (employee == null) return View(new List<SupplyOrder>());

            var myOrders = await _context.SupplyOrders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Item)
                .Where(o => o.EmployeeId == employee.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(myOrders);
        }

        // ==========================================
        // 2. ส่วนสำหรับ Admin (อนุมัติใบเบิก)
        // ==========================================

        [Authorize(Roles = @"CRIMCAD\CIS_Admins,CRIMCAD\Supply_Managers")]
        public async Task<IActionResult> ManageOrders()
        {
            var orders = await _context.SupplyOrders
                .Include(o => o.Requester)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Item)
                .OrderBy(o => o.Status)
                .ThenByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [Authorize(Roles = @"CRIMCAD\CIS_Admins,CRIMCAD\Supply_Managers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOrder(int id, string adminRemark)
        {
            var order = await _context.SupplyOrders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Item)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (order.Status != SupplyStatus.Pending)
            {
                return Json(new { success = false, message = "รายการนี้ถูกดำเนินการไปแล้ว" });
            }

            // ตัดสต็อก
            foreach (var detail in order.OrderItems)
            {
                var itemInDb = await _context.SupplyItems.FindAsync(detail.SupplyItemId);
                if (itemInDb != null)
                {
                    if (itemInDb.StockQuantity < detail.Quantity)
                    {
                        return Json(new { success = false, message = $"สินค้า '{itemInDb.ItemName}' มีไม่พอ (เหลือ {itemInDb.StockQuantity})" });
                    }
                    itemInDb.StockQuantity -= detail.Quantity;
                }
            }

            // ในส่วนผู้อนุมัติ (Approver) ก็ควรตัด Domain ออกเหมือนกันถ้าต้องการเก็บแบบสั้น
            string approverName = User.Identity.Name;
            if (!string.IsNullOrEmpty(approverName) && approverName.Contains("\\"))
            {
                approverName = approverName.Split('\\')[1];
            }

            order.Status = SupplyStatus.Approved;
            order.ApprovedBy = approverName; // บันทึกชื่อคนอนุมัติแบบสั้น
            order.ApprovedDate = DateTime.Now;
            order.AdminRemark = adminRemark;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "อนุมัติและตัดสต็อกเรียบร้อย" });
        }

        [HttpPost]
        [Authorize(Roles = @"CRIMCAD\CIS_Admins,CRIMCAD\Supply_Managers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOrder(int id, string adminRemark)
        {
            var order = await _context.SupplyOrders.FindAsync(id);
            if (order == null) return NotFound();

            // ตัดชื่อคนไม่อนุมัติให้สั้นด้วย
            string rejectorName = User.Identity.Name;
            if (!string.IsNullOrEmpty(rejectorName) && rejectorName.Contains("\\"))
            {
                rejectorName = rejectorName.Split('\\')[1];
            }

            order.Status = SupplyStatus.Rejected;
            order.ApprovedBy = rejectorName;
            order.ApprovedDate = DateTime.Now;
            order.AdminRemark = adminRemark;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "ไม่อนุมัติรายการนี้เรียบร้อย" });
        }

        // ==========================================
        // 3. ส่วนจัดการ Stock (CRUD สินค้า)
        // ==========================================

        [Authorize(Roles = @"CRIMCAD\CIS_Admins,CRIMCAD\Supply_Managers")]
        public async Task<IActionResult> StockIndex()
        {
            return View(await _context.SupplyItems.ToListAsync());
        }

        [Authorize(Roles = @"CRIMCAD\CIS_Admins,CRIMCAD\Supply_Managers")]
        public IActionResult CreateItem()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = @"CRIMCAD\CIS_Admins,CRIMCAD\Supply_Managers")]
        public async Task<IActionResult> CreateItem(SupplyItem item)
        {
            if (ModelState.IsValid)
            {
                _context.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(StockIndex));
            }
            return View(item);
        }

        [Authorize(Roles = @"CRIMCAD\CIS_Admins,CRIMCAD\Supply_Managers")]
        public async Task<IActionResult> EditItem(int? id)
        {
            if (id == null) return NotFound();
            var item = await _context.SupplyItems.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = @"CRIMCAD\CIS_Admins,CRIMCAD\Supply_Managers")]
        public async Task<IActionResult> EditItem(int id, SupplyItem item)
        {
            if (id != item.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(StockIndex));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.SupplyItems.Any(e => e.Id == id)) return NotFound();
                    else throw;
                }
            }
            return View(item);
        }
    }
}