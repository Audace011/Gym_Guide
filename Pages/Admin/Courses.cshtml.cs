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
    public class CoursesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CoursesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TrainingCourse> Courses { get; set; } = new();

        public async Task OnGetAsync()
        {
            Courses = await _context.TrainingCourses
                .Include(c => c.CourseExercises)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var course = await _context.TrainingCourses
                .Include(c => c.CourseExercises)
                .Include(c => c.MemberCourses)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course != null)
            {
                _context.CourseExercises.RemoveRange(course.CourseExercises);
                _context.MemberCourses.RemoveRange(course.MemberCourses);
                _context.TrainingCourses.Remove(course);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Course deleted successfully!";
            }

            return RedirectToPage("/Admin/Courses");
        }
    }
}
