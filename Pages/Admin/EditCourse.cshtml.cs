using System.Threading.Tasks;
using GymGuide.Data;
using GymGuide.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EditCourseModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditCourseModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TrainingCourse Course { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var course = await _context.TrainingCourses.FindAsync(id);
            if (course == null) return NotFound();

            Course = course;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.TrainingCourses.Update(Course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Course updated successfully!";
            return RedirectToPage("/Admin/Courses");
        }
    }
}
