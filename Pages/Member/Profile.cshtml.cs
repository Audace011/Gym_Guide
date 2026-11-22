using GymGuide.Data;
using GymGuide.Models;
using GymGuide.Services;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Pages.Member
{
    [Authorize(Roles = "Member")]
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IBmiService _bmiService;
        private readonly ITrainerAssignmentService _trainerAssignmentService;

        public ProfileModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IBmiService bmiService,
            ITrainerAssignmentService trainerAssignmentService)
        {
            _context = context;
            _userManager = userManager;
            _bmiService = bmiService;
            _trainerAssignmentService = trainerAssignmentService;
        }

        [BindProperty]
        public ProfileViewModel Input { get; set; } = new();

        private async Task<GymGuide.Models.Member?> GetCurrentMemberAsync()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return null;

            var userEntity = await _context.Users
                .Include(u => u.Member)
                    .ThenInclude(m => m!.Trainer)
                        .ThenInclude(t => t!.User)
                .Include(u => u.Member)
                    .ThenInclude(m => m!.WeightLogs)
                .Include(u => u.Member)
                    .ThenInclude(m => m!.TrainerRequests)
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id || u.Email == identityUser.Email);

            return userEntity?.Member;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound("Member record not found.");

            var latestLog = member.WeightLogs.OrderByDescending(w => w.LoggedAt).FirstOrDefault();
            var latestRequest = member.TrainerRequests.OrderByDescending(r => r.RequestedAt).FirstOrDefault();

            Input = new ProfileViewModel
            {
                MemberId = member.MemberId,
                FullName = member.User?.FullName ?? "",
                Email = member.User?.Email ?? "",
                PhoneNumber = member.User?.PhoneNumber,
                DateOfBirth = member.DateOfBirth,
                Gender = member.Gender,
                HeightCm = member.HeightCm,
                Goal = member.Goal,
                LatestWeightKg = latestLog?.WeightKg ?? 0,
                LatestBmi = latestLog?.Bmi ?? 0,
                LatestBmiCategory = latestLog?.Bmicategory ?? "Normal",
                AssignedTrainerName = member.Trainer?.User?.FullName,
                AssignedTrainerSpecialization = member.Trainer?.Specialization,
                LatestTrainerRequestStatus = latestRequest?.Status
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound();

            if (!ModelState.IsValid)
            {
                Input.FullName = member.User?.FullName ?? "";
                Input.Email = member.User?.Email ?? "";
                return Page();
            }

            member.DateOfBirth = Input.DateOfBirth;
            member.Gender = Input.Gender;
            member.HeightCm = Input.HeightCm;
            member.Goal = Input.Goal;

            var latestLog = member.WeightLogs.OrderByDescending(w => w.LoggedAt).FirstOrDefault();
            if (latestLog != null && Input.HeightCm.HasValue)
            {
                latestLog.Bmi = _bmiService.CalculateBmi(Input.HeightCm.Value, latestLog.WeightKg);
                latestLog.Bmicategory = _bmiService.GetBmiCategory(latestLog.Bmi);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile details updated successfully!";
            return RedirectToPage("/Member/Profile");
        }

        public async Task<IActionResult> OnPostRequestTrainerAsync()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound();

            var result = await _trainerAssignmentService.RequestTrainerAsync(member.MemberId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["WarningMessage"] = result.Message;
            }

            return RedirectToPage("/Member/Profile");
        }
    }
}
