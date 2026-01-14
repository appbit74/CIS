using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CIS.Data;
using CIS.Models;

namespace CIS.Controllers
{
    public class DivisionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DivisionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Divisions (รายการส่วนงาน)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Divisions.ToListAsync());
        }

        // GET: Divisions/Create (หน้าเพิ่มข้อมูล)
        public IActionResult Create()
        {
            return View();
        }

        // POST: Divisions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Division division)
        {
            if (ModelState.IsValid)
            {
                _context.Add(division);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(division);
        }

        // GET: Divisions/Edit/5 (หน้าแก้ไข)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var division = await _context.Divisions.FindAsync(id);
            if (division == null) return NotFound();
            return View(division);
        }

        // POST: Divisions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Division division)
        {
            if (id != division.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(division);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Divisions.Any(e => e.Id == division.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(division);
        }

        // GET: Divisions/Delete/5 (หน้าลบ)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var division = await _context.Divisions.FirstOrDefaultAsync(m => m.Id == id);
            if (division == null) return NotFound();

            return View(division);
        }

        // POST: Divisions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var division = await _context.Divisions.FindAsync(id);
            if (division != null)
            {
                _context.Divisions.Remove(division);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}