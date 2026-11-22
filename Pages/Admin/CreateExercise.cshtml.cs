using System.Threading.Tasks;
using GymGuide.Data;
using GymGuide.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CreateExerciseModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateExerciseModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Exercise Exercise { get; set; } = new()
        {
            MuscleGroup = "Chest"
        };

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Exercises.Add(Exercise);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Exercise created successfully!";
            return RedirectToPage("/Admin/Exercises");
        }
    }
}
