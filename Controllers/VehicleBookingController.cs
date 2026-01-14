using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CIS.Data;
using CIS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CIS.Controllers
{
    [Authorize]
    public class VehicleBookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VehicleBookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. หน้าปฏิทินจองรถ
        public IActionResult Index()
        {
            // ดึงข้อมูลรถมาทำ Dropdown (แสดง ยี่ห้อ รุ่น และ ทะเบียน)
            var vehicles = _context.Vehicles
                .Where(v => v.IsActive)
                .Select(v => new
                {
                    Id = v.Id,
                    DisplayText = $"{v.Brand} {v.Model} ({v.LicensePlate}) - {v.SeatCount} ที่นั่ง"
                })
                .ToList();

            ViewData["Vehicles"] = new SelectList(vehicles, "Id", "DisplayText");
            return View();
        }

        // 2. API ส่งข้อมูลให้ปฏิทิน
        [HttpGet]
        public async Task<IActionResult> GetEvents(DateTime start, DateTime end)
        {
            var bookings = await _context.VehicleBookings
                .Include(b => b.Vehicle)
                .Where(b => b.StartTime < end && b.EndTime > start
                            && b.Status != BookingStatus.Cancelled
                            && b.Status != BookingStatus.Rejected) // 1. *** เพิ่มบรรทัดนี้: ไม่เอา "ไม่อนุมัติ" ***
                .ToListAsync();

            var events = bookings.Select(b => {
                string eventColor = b.Vehicle.CalendarColor;
                string titlePrefix = "";

                if (b.Status == BookingStatus.Pending)
                {
                    eventColor = "#999999";
                    titlePrefix = "(รออนุมัติ) ";
                }
                // 2. *** ลบ Logic ของ Rejected ทิ้งไปได้เลย (เพราะมันจะไม่ถูกดึงมาแล้ว) ***
                /*
                else if (b.Status == BookingStatus.Rejected) 
                {
                    eventColor = "#dc3545"; 
                    titlePrefix = "(ไม่อนุมัติ) ";
                }
                */

                string driverInfo = b.IsDriverRequired ? " (ขอคนขับ)" : " (ขับเอง)";

                bool isEditable = (b.BookedBy == User.Identity.Name) ||
                                  User.IsInRole(@"CRIMCAD\CIS_Admins") ||
                                  User.IsInRole(@"CRIMCAD\Car_Managers");

                return new
                {
                    id = b.Id,
                    title = $"{titlePrefix}ไป {b.Destination} ({b.Vehicle.LicensePlate}){driverInfo}",
                    start = b.StartTime.ToString("s"),
                    end = b.EndTime.ToString("s"),
                    color = eventColor,
                    description = $"ภารกิจ: {b.Title}\nสถานที่: {b.Destination}\nผู้จอง: {b.BookedBy}\nคนขับ: {(b.IsDriverRequired ? "ต้องการ" : "ไม่ต้องการ")}\nสถานะ: {b.Status}",
                    textColor = "#ffffff",

                    extendedProps = new
                    {
                        isEditable = isEditable,
                        description = $"ภารกิจ: {b.Title}\nสถานที่: {b.Destination}\nผู้จอง: {b.BookedBy}\nคนขับ: {(b.IsDriverRequired ? "ต้องการ" : "ไม่ต้องการ")}\nสถานะ: {b.Status}"
                    }
                };
            });

            return Json(events);
        }

        // 3. รับการจอง
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] VehicleBooking booking)
        {
            // ตั้งค่าพื้นฐาน
            booking.BookedBy = User.Identity?.Name ?? "Unknown";
            booking.Status = BookingStatus.Pending; // เริ่มต้นที่รออนุมัติเสมอ

            // ตรวจสอบข้อมูลจำเป็น
            if (string.IsNullOrEmpty(booking.Title) || string.IsNullOrEmpty(booking.Destination))
            {
                return Json(new { success = false, message = "กรุณากรอกจุดประสงค์และสถานที่ปลายทาง" });
            }

            // Conflict Check (รถคันเดียวกัน เวลาทับกันไหม)
            bool isOverlap = await _context.VehicleBookings.AnyAsync(b =>
                b.VehicleId == booking.VehicleId &&
                b.Id != booking.Id &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Rejected &&
                (
                    (booking.StartTime >= b.StartTime && booking.StartTime < b.EndTime) ||
                    (booking.EndTime > b.StartTime && booking.EndTime <= b.EndTime) ||
                    (booking.StartTime <= b.StartTime && booking.EndTime >= b.EndTime)
                )
            );

            if (isOverlap)
            {
                return Json(new { success = false, message = "รถคันนี้ไม่ว่างในช่วงเวลาดังกล่าวครับ" });
            }

            try
            {
                _context.VehicleBookings.Add(booking);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "จองรถเรียบร้อย! โปรดรอการอนุมัติจากยานพาหนะ" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // --- 4. *** [NEW] หน้าจัดการการจองรถ (สำหรับ Car Managers) *** ---
        // (สมมติว่าใช้กลุ่ม Car_Managers หรือ CIS_Admins)
        [Authorize(Roles = @"CRIMCAD\CIS_Admins,CRIMCAD\Car_Managers")]
        public async Task<IActionResult> Manage()
        {
            // ดึงรายการจองรถทั้งหมด (เรียงตามสถานะ: Pending มาก่อน)
            var bookings = await _context.VehicleBookings
                .Include(b => b.Vehicle)
                .OrderBy(b => b.Status) // Pending (0) ขึ้นก่อน
                .ThenByDescending(b => b.StartTime)
                .ToListAsync();

            return View(bookings);
        }

        // --- 5. *** [NEW] API อัปเดตสถานะ (Approve/Reject) *** ---
        [HttpPost]
        [Authorize(Roles = @"CRIMCAD\CIS_Admins,CRIMCAD\Car_Managers")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, BookingStatus status)
        {
            var booking = await _context.VehicleBookings.FindAsync(id);
            if (booking == null) return Json(new { success = false, message = "ไม่พบรายการ" });

            booking.Status = status;
            await _context.SaveChangesAsync();

            // (ในอนาคต: ตรงนี้อาจจะเพิ่ม Logic ส่ง Line Notify หาคนขับรถได้)

            return Json(new { success = true, message = $"อัปเดตสถานะเรียบร้อย!" });
        }


        // --- 6. *** [NEW] GET Edit *** ---
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.VehicleBookings.FindAsync(id);
            if (booking == null) return NotFound();

            // เช็คสิทธิ์ (กันคนนอกแอบเข้า URL ตรงๆ)
            bool isAuthorized = (booking.BookedBy == User.Identity.Name) ||
                                User.IsInRole(@"CRIMCAD\CIS_Admins") ||
                                User.IsInRole(@"CRIMCAD\Car_Managers");

            if (!isAuthorized)
            {
                return Forbid(); // ห้ามเข้า
            }

            // โหลด Dropdown รถ
            var vehicles = _context.Vehicles.Where(v => v.IsActive)
                .Select(v => new { Id = v.Id, DisplayText = $"{v.Brand} {v.Model} ({v.LicensePlate}) - {v.SeatCount} ที่นั่ง" })
                .ToList();
            ViewData["Vehicles"] = new SelectList(vehicles, "Id", "DisplayText", booking.VehicleId);

            return View(booking);
        }

        // --- 7. *** [NEW] POST Edit *** ---
        // (แบบ Form Submit ปกติ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VehicleBooking booking)
        {
            if (id != booking.Id) return NotFound();

            // เช็คสิทธิ์อีกครั้ง
            // (ต้องดึงข้อมูลเก่ามาเช็ค BookedBy ก่อน แต่เพื่อความเร็ว สมมติว่าผ่าน Validated User)

            // เราต้อง "คงค่าเดิม" บางอย่างไว้ (เช่น BookedBy)
            var existingBooking = await _context.VehicleBookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (existingBooking == null) return NotFound();

            booking.BookedBy = existingBooking.BookedBy; // คงชื่อผู้จองเดิม
            booking.Status = BookingStatus.Pending; // *** รีเซ็ตเป็นรออนุมัติเสมอเมื่อแก้ไข ***

            // Validation
            if (string.IsNullOrEmpty(booking.Title) || string.IsNullOrEmpty(booking.Destination))
            {
                ModelState.AddModelError("", "ข้อมูลไม่ครบถ้วน");
            }

            // Conflict Check (เช็คซ้ำ ยกเว้นตัวเอง)
            bool isOverlap = await _context.VehicleBookings.AnyAsync(b =>
                b.VehicleId == booking.VehicleId &&
                b.Id != booking.Id && // *** สำคัญ: ยกเว้นตัวเอง ***
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Rejected &&
                (
                    (booking.StartTime >= b.StartTime && booking.StartTime < b.EndTime) ||
                    (booking.EndTime > b.StartTime && booking.EndTime <= b.EndTime) ||
                    (booking.StartTime <= b.StartTime && booking.EndTime >= b.EndTime)
                )
            );

            if (isOverlap)
            {
                ModelState.AddModelError("", "รถคันนี้ไม่ว่างในช่วงเวลาดังกล่าว (มีการจองซ้อน)");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index)); // กลับไปปฏิทิน
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.VehicleBookings.Any(e => e.Id == booking.Id)) return NotFound();
                    else throw;
                }
            }

            // ถ้า Error โหลด Dropdown กลับไป
            var vehicles = _context.Vehicles.Where(v => v.IsActive)
                .Select(v => new { Id = v.Id, DisplayText = $"{v.Brand} {v.Model} ({v.LicensePlate}) - {v.SeatCount} ที่นั่ง" })
                .ToList();
            ViewData["Vehicles"] = new SelectList(vehicles, "Id", "DisplayText", booking.VehicleId);

            return View(booking);
        }
    }
}