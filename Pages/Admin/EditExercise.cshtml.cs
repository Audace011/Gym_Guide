using System.Threading.Tasks;
using GymGuide.Data;
using GymGuide.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EditExerciseModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditExerciseModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Exercise Exercise { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var exercise = await _context.Exercises.FindAsync(id);
            if (exercise == null) return NotFound();

            Exercise = exercise;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Exercises.Update(Exercise);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Exercise updated successfully!";
            return RedirectToPage("/Admin/Exercises");
        }
    }
}
