using GymGuide.Data;
using GymGuide.Models;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Pages.Trainer
{
    [Authorize(Roles = "Trainer,Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public TrainerDashboardViewModel Dashboard { get; set; } = new();

        private async Task<GymGuide.Models.Trainer?> GetCurrentTrainerAsync()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return null;

            var userEntity = await _context.Users
                .Include(u => u.Trainer)
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id || u.Email == identityUser.Email);

            return userEntity?.Trainer;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            GymGuide.Models.Trainer? trainer = null;
            if (User.IsInRole("Trainer"))
            {
                trainer = await GetCurrentTrainerAsync();
                if (trainer == null)
                {
                    return Page();
                }
            }
            else
            {
                // Admin viewing trainer dashboard: pick first trainer
                trainer = await _context.Trainers.Include(t => t.User).FirstOrDefaultAsync();
                if (trainer == null)
                {
                    return Page();
                }
            }

            var membersList = await _context.Members
                .Include(m => m.User)
                .Include(m => m.WeightLogs)
                .Include(m => m.MemberCourses)
                    .ThenInclude(mc => mc.Course)
                .Include(m => m.MemberCourses)
                    .ThenInclude(mc => mc.SessionLogs)
                .Where(m => m.TrainerId == trainer.TrainerId)
                .ToListAsync();

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

            Dashboard = new TrainerDashboardViewModel
            {
                Trainer = trainer,
                AssignedMembers = assignedVms
            };

            return Page();
        }
    }
}
