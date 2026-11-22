using GymGuide.Data;
using GymGuide.Models;
using GymGuide.Services;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Pages.Trainer
{
    [Authorize(Roles = "Trainer,Admin")]
    public class MemberDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ITrainerAssignmentService _trainerAssignmentService;

        public MemberDetailsModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ITrainerAssignmentService trainerAssignmentService)
        {
            _context = context;
            _userManager = userManager;
            _trainerAssignmentService = trainerAssignmentService;
        }

        public TrainerMemberDetailViewModel Detail { get; set; } = new();

        private async Task<GymGuide.Models.Trainer?> GetCurrentTrainerAsync()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return null;

            var userEntity = await _context.Users
                .Include(u => u.Trainer)
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id || u.Email == identityUser.Email);

            return userEntity?.Trainer;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var member = await _context.Members
                .Include(m => m.User)
                .Include(m => m.WeightLogs)
                .Include(m => m.MemberCourses)
                    .ThenInclude(mc => mc.Course)
                .Include(m => m.MemberCourses)
                    .ThenInclude(mc => mc.SessionLogs)
                .FirstOrDefaultAsync(m => m.MemberId == id);

            if (member == null) return NotFound("Member not found.");

            if (User.IsInRole("Trainer") && !User.IsInRole("Admin"))
            {
                var currentTrainer = await GetCurrentTrainerAsync();
                if (currentTrainer == null || member.TrainerId != currentTrainer.TrainerId)
                {
                    return Forbid();
                }
            }

            var activeCourse = member.MemberCourses.FirstOrDefault(mc => mc.Status == "Active");
            var allCourses = await _context.TrainingCourses.OrderBy(c => c.Name).ToListAsync();

            Detail = new TrainerMemberDetailViewModel
            {
                Member = member,
                WeightHistory = member.WeightLogs.OrderByDescending(w => w.LoggedAt).ToList(),
                ActiveCourse = activeCourse,
                AvailableCourses = allCourses,
                CompletedSessionsCount = activeCourse?.SessionLogs.Count(sl => sl.Completed) ?? 0
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAdjustWorkoutPlanAsync(int memberId, int newCourseId)
        {
            var trainer = await GetCurrentTrainerAsync();
            int trainerId = trainer?.TrainerId ?? 0;

            if (User.IsInRole("Admin") && trainerId == 0)
            {
                var member = await _context.Members.FindAsync(memberId);
                trainerId = member?.TrainerId ?? 1;
            }

            bool adjusted = await _trainerAssignmentService.AdjustWorkoutPlanAsync(trainerId, memberId, newCourseId);

            if (adjusted)
            {
                TempData["SuccessMessage"] = "Workout plan successfully adjusted! Old course set to Replaced and new course activated.";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not adjust workout plan. Please ensure member and course are valid.";
            }

            return RedirectToPage("/Trainer/MemberDetails", new { id = memberId });
        }
    }
}
