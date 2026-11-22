using System.Threading.Tasks;
using GymGuide.Data;
using GymGuide.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CreateCourseModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateCourseModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TrainingCourse Course { get; set; } = new()
        {
            Level = "Beginner",
            DurationWeeks = 8,
            Bmicategory = "Normal",
            Goal = "Stay fit"
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

            _context.TrainingCourses.Add(Course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Course created successfully!";
            return RedirectToPage("/Admin/Courses");
        }
    }
}
