using GymGuide.Data;
using GymGuide.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class TrainersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TrainersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<GymGuide.Models.Trainer> Trainers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Trainers = await _context.Trainers
                .Include(t => t.User)
                .Include(t => t.Members)
                .ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostToggleAvailabilityAsync(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer != null)
            {
                trainer.IsAvailable = !trainer.IsAvailable;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Trainer availability updated to {(trainer.IsAvailable ? "Available" : "Unavailable")}.";
            }
            return RedirectToPage("/Admin/Trainers");
        }

        public async Task<IActionResult> OnPostDeleteTrainerAsync(int id)
        {
            var trainer = await _context.Trainers
                .Include(t => t.Members)
                .Include(t => t.TrainerRequests)
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TrainerId == id);

            if (trainer != null)
            {
                foreach (var m in trainer.Members)
                {
                    m.TrainerId = null;
                }

                _context.TrainerRequests.RemoveRange(trainer.TrainerRequests);
                _context.Trainers.Remove(trainer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Trainer record removed successfully!";
            }

            return RedirectToPage("/Admin/Trainers");
        }
    }
}
