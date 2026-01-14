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
using CIS.ViewModels; // <--- เพิ่มบรรทัดนี้

namespace CIS.Controllers
{
    [Authorize]
    public class SupplyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupplyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. ส่วนสำหรับ User (เบิกของ)
        // ==========================================

        // GET: Supply/Index (หน้า Catalog เลือกของ)
        public async Task<IActionResult> Index()
        {
            // ดึงเฉพาะของที่เปิดให้เบิก (IsActive) และเรียงตามชื่อ
            var items = await _context.SupplyItems
                .Where(i => i.IsActive)
                .OrderBy(i => i.ItemName)
                .ToListAsync();

            return View(items);
        }

        // POST: Supply/CreateOrder (รับ Data JSON จากหน้าบ้าน)
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequestViewModel model)
        {
            if (model == null || model.Items == null || !model.Items.Any())
            {
                return Json(new { success = false, message = "ไม่พบรายการวัสดุที่เลือก" });
            }

            // 1. หา EmployeeId ของคน Login
            // (สมมติว่า GeneratedUsername เก็บ User AD เช่น "DOMAIN\User")
            string currentUsername = User.Identity.Name;
            var employee = await _context.EmployeeProfiles
                .FirstOrDefaultAsync(e => e.GeneratedUsername == currentUsername);

            if (employee == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลพนักงานของคุณในระบบ HR" });
            }

            // 2. สร้าง Header ใบเบิก
            var order = new SupplyOrder
            {
                EmployeeId = employee.Id,
                OrderDate = DateTime.Now,
                Status = SupplyStatus.Pending,
                UserRemark = model.Remark,
                OrderItems = new List<SupplyOrderItem>()
            };

            // 3. วนลูปสร้าง Detail
            foreach (var reqItem in model.Items)
            {
                // ตรวจสอบสต็อกเบื้องต้น (Optional: อาจจะไม่เช็คก็ได้ถ้าอยากให้เบิกเกินไว้ก่อน)
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

        // GET: Supply/MyHistory (ดูประวัติการเบิกของตัวเอง)
        public async Task<IActionResult> MyHistory()
        {
            string currentUsername = User.Identity.Name;
            var employee = await _context.EmployeeProfiles
                .FirstOrDefaultAsync(e => e.GeneratedUsername == currentUsername);

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
                .Include(o => o.Requester) // ดึงชื่อคนเบิก
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Item) // ดึงชื่อของ
                .OrderBy(o => o.Status) // เอา Pending ขึ้นก่อน
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

            // *** Logic ตัดสต็อก ***
            foreach (var detail in order.OrderItems)
            {
                var itemInDb = await _context.SupplyItems.FindAsync(detail.SupplyItemId);
                if (itemInDb != null)
                {
                    // ถ้าของไม่พอ
                    if (itemInDb.StockQuantity < detail.Quantity)
                    {
                        return Json(new { success = false, message = $"สินค้า '{itemInDb.ItemName}' มีไม่พอ (เหลือ {itemInDb.StockQuantity})" });
                    }

                    // ตัดของ
                    itemInDb.StockQuantity -= detail.Quantity;
                }
            }

            // Update Status
            order.Status = SupplyStatus.Approved; // หรือ Completed ถ้าถือว่ารับเลย
            order.ApprovedBy = User.Identity.Name;
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

            order.Status = SupplyStatus.Rejected;
            order.ApprovedBy = User.Identity.Name;
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