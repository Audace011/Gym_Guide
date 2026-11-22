using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymGuide.Data;
using GymGuide.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ExercisesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ExercisesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Exercise> Exercises { get; set; } = new();

        public async Task OnGetAsync()
        {
            Exercises = await _context.Exercises.OrderBy(e => e.Name).ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var exercise = await _context.Exercises
                .Include(e => e.CourseExercises)
                .FirstOrDefaultAsync(e => e.ExerciseId == id);

            if (exercise != null)
            {
                _context.CourseExercises.RemoveRange(exercise.CourseExercises);
                _context.Exercises.Remove(exercise);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Exercise deleted successfully!";
            }

            return RedirectToPage("/Admin/Exercises");
        }
    }
}
