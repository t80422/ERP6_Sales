using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ERP6.Models;

namespace ERP6.Controllers
{
    public class Acccv10Controller : Controller
    {
        private readonly EEPEF01Context _context;

        public Acccv10Controller(EEPEF01Context context)
        {
            _context = context;
        }

        // GET: Acccv10
        public async Task<IActionResult> Index()
        {
            return View(await _context.Acccv10.ToListAsync());
        }

        // GET: Acccv10/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var acccv10 = await _context.Acccv10
                .FirstOrDefaultAsync(m => m.Subno == id);
            if (acccv10 == null)
            {
                return NotFound();
            }

            return View(acccv10);
        }

        // GET: Acccv10/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Acccv10/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Subno,Subname")] Acccv10 acccv10)
        {
            if (ModelState.IsValid)
            {
                _context.Add(acccv10);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(acccv10);
        }

        // GET: Acccv10/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var acccv10 = await _context.Acccv10.FindAsync(id);
            if (acccv10 == null)
            {
                return NotFound();
            }
            return View(acccv10);
        }

        // POST: Acccv10/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Subno,Subname")] Acccv10 acccv10)
        {
            if (id != acccv10.Subno)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(acccv10);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Acccv10Exists(acccv10.Subno))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(acccv10);
        }

        // GET: Acccv10/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var acccv10 = await _context.Acccv10
                .FirstOrDefaultAsync(m => m.Subno == id);
            if (acccv10 == null)
            {
                return NotFound();
            }

            return View(acccv10);
        }

        // POST: Acccv10/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var acccv10 = await _context.Acccv10.FindAsync(id);
            _context.Acccv10.Remove(acccv10);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool Acccv10Exists(string id)
        {
            return _context.Acccv10.Any(e => e.Subno == id);
        }
    }
}
