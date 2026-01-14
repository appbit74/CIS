using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CIS.Data;
using CIS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CIS.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewData["MeetingRooms"] = new SelectList(_context.MeetingRooms.Where(r => r.IsActive), "Id", "Name");
            return View();
        }

        // 1. API ส่งข้อมูลให้ปฏิทิน (อัปเกรดเรื่องสี)
        [HttpGet]
        public async Task<IActionResult> GetEvents(DateTime start, DateTime end)
        {
            var bookings = await _context.RoomBookings
                .Include(b => b.MeetingRoom)
                .Where(b => b.StartTime < end && b.EndTime > start && b.Status != BookingStatus.Cancelled) // ไม่ดึงที่ยกเลิก
                .ToListAsync();

            var events = bookings.Select(b => {
                // *** Logic เลือกสี ***
                string eventColor = b.MeetingRoom.Color;
                string titlePrefix = "";

                if (b.Status == BookingStatus.Pending)
                {
                    eventColor = "#999999"; // สีเทา (รออนุมัติ)
                    titlePrefix = "(รออนุมัติ) ";
                }
                else if (b.Status == BookingStatus.Rejected)
                {
                    // (ปกติเราจะไม่ส่ง Rejected ไปแสดง หรืออาจจะแสดงเป็นสีแดงขีดฆ่า)
                    eventColor = "#dc3545";
                    titlePrefix = "(ไม่อนุมัติ) ";
                }

                return new
                {
                    id = b.Id,
                    title = $"{titlePrefix}{b.Title} ({b.MeetingRoom.Name})",
                    start = b.StartTime.ToString("s"),
                    end = b.EndTime.ToString("s"),
                    color = eventColor,
                    description = $"จองโดย: {b.BookedBy}\nสถานะ: {b.Status}",
                    textColor = "#ffffff"
                };
            });

            return Json(events);
        }

        // 2. รับการจอง (อัปเกรดสถานะเริ่มต้น)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] RoomBooking booking)
        {
            booking.BookedBy = User.Identity?.Name ?? "Unknown";

            // *** เปลี่ยนสถานะเป็น Pending (รออนุมัติ) ***
            booking.Status = BookingStatus.Pending;

            // Conflict Check (เหมือนเดิม)
            // (เช็คเฉพาะรายการที่ Approved หรือ Pending)
            bool isOverlap = await _context.RoomBookings.AnyAsync(b =>
                b.MeetingRoomId == booking.MeetingRoomId &&
                b.Id != booking.Id &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Rejected && // ไม่นับรายการที่ถูกปฏิเสธ
                (
                    (booking.StartTime >= b.StartTime && booking.StartTime < b.EndTime) ||
                    (booking.EndTime > b.StartTime && booking.EndTime <= b.EndTime) ||
                    (booking.StartTime <= b.StartTime && booking.EndTime >= b.EndTime)
                )
            );

            if (isOverlap)
            {
                return Json(new { success = false, message = "ช่วงเวลานี้มีการจองรออยู่แล้ว (หรืออนุมัติแล้ว) กรุณาเลือกเวลาอื่น" });
            }

            try
            {
                _context.RoomBookings.Add(booking);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "ส่งคำขอจองเรียบร้อย! โปรดรอการอนุมัติจากผู้ดูแล" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // --- 3. *** [NEW] หน้าจัดการการจอง (สำหรับ Room Managers) *** ---
        [Authorize(Roles = @"CRIMCAD\Room_Managers,CRIMCAD\CIS_Admins")]
        public async Task<IActionResult> Manage()
        {
            // ดึงรายการที่ "รออนุมัติ" ขึ้นมาก่อน
            var bookings = await _context.RoomBookings
                .Include(b => b.MeetingRoom)
                .OrderBy(b => b.Status) // Pending (0) จะขึ้นก่อน
                .ThenByDescending(b => b.StartTime)
                .ToListAsync();

            return View(bookings);
        }

        // --- 4. *** [NEW] API อัปเดตสถานะ (Approve/Reject) *** ---
        [HttpPost]
        [Authorize(Roles = @"CRIMCAD\Room_Managers,CRIMCAD\CIS_Admins")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, BookingStatus status)
        {
            var booking = await _context.RoomBookings.FindAsync(id);
            if (booking == null) return Json(new { success = false, message = "ไม่พบรายการ" });

            booking.Status = status;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"อัปเดตสถานะเป็น {status} เรียบร้อย!" });
        }
    }
}