using System.Threading.Tasks;
using GymGuide.Data;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EditTrainerModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditTrainerModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TrainerEditViewModel Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var trainer = await _context.Trainers.Include(t => t.User).FirstOrDefaultAsync(t => t.TrainerId == id);
            if (trainer == null) return NotFound();

            Input = new TrainerEditViewModel
            {
                TrainerId = trainer.TrainerId,
                FullName = trainer.User?.FullName ?? "",
                Email = trainer.User?.Email ?? "",
                PhoneNumber = trainer.User?.PhoneNumber,
                Specialization = trainer.Specialization,
                MaxClients = trainer.MaxClients,
                IsAvailable = trainer.IsAvailable
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var trainer = await _context.Trainers.Include(t => t.User).FirstOrDefaultAsync(t => t.TrainerId == Input.TrainerId);
            if (trainer == null) return NotFound();

            if (trainer.User != null)
            {
                trainer.User.FullName = Input.FullName;
                trainer.User.PhoneNumber = Input.PhoneNumber;
            }

            trainer.Specialization = Input.Specialization;
            trainer.MaxClients = Input.MaxClients;
            trainer.IsAvailable = Input.IsAvailable;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Trainer {Input.FullName} updated successfully!";
            return RedirectToPage("/Admin/Trainers");
        }
    }
}
