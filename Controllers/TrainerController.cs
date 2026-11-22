using GymGuide.Data;
using GymGuide.Models;
using GymGuide.Services;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Controllers
{
    [Authorize(Roles = "Trainer,Admin")]
    public class TrainerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ITrainerAssignmentService _trainerAssignmentService;

        public TrainerController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ITrainerAssignmentService trainerAssignmentService)
        {
            _context = context;
            _userManager = userManager;
            _trainerAssignmentService = trainerAssignmentService;
        }

        private async Task<Trainer?> GetCurrentTrainerAsync()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return null;

            var userEntity = await _context.Users
                .Include(u => u.Trainer)
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id || u.Email == identityUser.Email);

            return userEntity?.Trainer;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            Trainer? trainer = null;
            if (User.IsInRole("Trainer"))
            {
                trainer = await GetCurrentTrainerAsync();
                if (trainer == null)
                    return View("Error", new ErrorViewModel { RequestId = "Trainer profile not linked to user." });
            }
            else
            {
                // Admin viewing trainer dashboard: pick first trainer
                trainer = await _context.Trainers.Include(t => t.User).FirstOrDefaultAsync();
                if (trainer == null)
                    return View(new TrainerDashboardViewModel());
            }

            var membersQuery = _context.Members
                .Include(m => m.User)
                .Include(m => m.WeightLogs)
                .Include(m => m.MemberCourses)
                    .ThenInclude(mc => mc.Course)
                .Include(m => m.MemberCourses)
                    .ThenInclude(mc => mc.SessionLogs)
                .Where(m => m.TrainerId == trainer.TrainerId);

            var membersList = await membersQuery.ToListAsync();

            var assignedVms = membersList.Select(m =>
            {
                var latestWeight = m.WeightLogs.OrderByDescending(w => w.LoggedAt).FirstOrDefault();
                var activeCourse = m.MemberCourses.FirstOrDefault(mc => mc.Status == "Active");

                return new TrainerMemberItemViewModel
                {
                    MemberId = m.MemberId,
                    FullName = m.User.FullName,
                    Email = m.User.Email,
                    PhoneNumber = m.User.PhoneNumber,
                    Goal = m.Goal,
                    LatestWeightKg = latestWeight?.WeightKg ?? 0,
                    LatestBmi = latestWeight?.Bmi ?? 0,
                    LatestBmiCategory = latestWeight?.Bmicategory ?? "N/A",
                    ActiveCourseName = activeCourse?.Course?.Name ?? "None",
                    CompletedSessionsCount = activeCourse?.SessionLogs.Count(sl => sl.Completed) ?? 0
                };
            }).ToList();

            var vm = new TrainerDashboardViewModel
            {
                Trainer = trainer,
                AssignedMembers = assignedVms
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> MemberDetails(int id)
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

            // Security check: non-admin trainers can only view their own members
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

            var vm = new TrainerMemberDetailViewModel
            {
                Member = member,
                WeightHistory = member.WeightLogs.OrderByDescending(w => w.LoggedAt).ToList(),
                ActiveCourse = activeCourse,
                AvailableCourses = allCourses,
                CompletedSessionsCount = activeCourse?.SessionLogs.Count(sl => sl.Completed) ?? 0
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustWorkoutPlan(int memberId, int newCourseId)
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

            return RedirectToAction(nameof(MemberDetails), new { id = memberId });
        }
    }
}
