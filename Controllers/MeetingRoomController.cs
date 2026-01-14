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

namespace CIS.Controllers
{
    // *** ล็อกสิทธิ์ให้เฉพาะ Admin เท่านั้นที่จัดการห้องได้ ***
    [Authorize(Roles = @"CRIMCAD\CIS_Admins")]
    public class MeetingRoomController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MeetingRoomController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MeetingRoom
        public async Task<IActionResult> Index()
        {
            return View(await _context.MeetingRooms.ToListAsync());
        }

        // GET: MeetingRoom/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var meetingRoom = await _context.MeetingRooms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (meetingRoom == null) return NotFound();

            return View(meetingRoom);
        }

        // GET: MeetingRoom/Create
        public IActionResult Create()
        {
            // ค่าเริ่มต้น
            return View(new MeetingRoom { Color = "#3788d8", IsActive = true });
        }

        // POST: MeetingRoom/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Location,Capacity,Color,IsActive")] MeetingRoom meetingRoom)
        {
            if (ModelState.IsValid)
            {
                _context.Add(meetingRoom);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(meetingRoom);
        }

        // GET: MeetingRoom/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var meetingRoom = await _context.MeetingRooms.FindAsync(id);
            if (meetingRoom == null) return NotFound();
            return View(meetingRoom);
        }

        // POST: MeetingRoom/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Location,Capacity,Color,IsActive")] MeetingRoom meetingRoom)
        {
            if (id != meetingRoom.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(meetingRoom);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MeetingRoomExists(meetingRoom.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(meetingRoom);
        }

        // GET: MeetingRoom/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var meetingRoom = await _context.MeetingRooms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (meetingRoom == null) return NotFound();

            return View(meetingRoom);
        }

        // POST: MeetingRoom/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var meetingRoom = await _context.MeetingRooms.FindAsync(id);
            if (meetingRoom != null)
            {
                _context.MeetingRooms.Remove(meetingRoom);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MeetingRoomExists(int id)
        {
            return _context.MeetingRooms.Any(e => e.Id == id);
        }
    }
}