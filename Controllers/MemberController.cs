using GymGuide.Data;
using GymGuide.Models;
using GymGuide.Services;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GymGuide.Controllers
{
    [Authorize(Roles = "Member")]
    public class MemberController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IBmiService _bmiService;
        private readonly ITrainerAssignmentService _trainerAssignmentService;

        public MemberController(
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

        private async Task<Member?> GetCurrentMemberAsync()
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

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound("Member record not found.");

            var latestLog = member.WeightLogs.OrderByDescending(w => w.LoggedAt).FirstOrDefault();
            var latestRequest = member.TrainerRequests.OrderByDescending(r => r.RequestedAt).FirstOrDefault();

            var vm = new ProfileViewModel
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

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.FullName = member.User?.FullName ?? "";
                model.Email = member.User?.Email ?? "";
                return View(model);
            }

            member.DateOfBirth = model.DateOfBirth;
            member.Gender = model.Gender;
            member.HeightCm = model.HeightCm;
            member.Goal = model.Goal;

            // Recalculate BMI on latest weight if height changed
            var latestLog = member.WeightLogs.OrderByDescending(w => w.LoggedAt).FirstOrDefault();
            if (latestLog != null && model.HeightCm.HasValue)
            {
                latestLog.Bmi = _bmiService.CalculateBmi(latestLog.WeightKg, model.HeightCm.Value);
                latestLog.Bmicategory = _bmiService.GetBmiCategory(latestLog.Bmi);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile details updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> WeightTracker()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound();

            var logs = member.WeightLogs.OrderByDescending(w => w.LoggedAt).ToList();
            var latestLog = logs.FirstOrDefault();

            // Prepare chart chronological data
            var chronological = logs.OrderBy(w => w.LoggedAt).ToList();
            var dates = chronological.Select(w => w.LoggedAt.ToString("MMM dd, yyyy")).ToArray();
            var weights = chronological.Select(w => w.WeightKg).ToArray();

            var vm = new WeightTrackingViewModel
            {
                CurrentBmi = latestLog?.Bmi ?? 0,
                CurrentBmiCategory = latestLog?.Bmicategory ?? "N/A",
                NewWeightKg = latestLog?.WeightKg ?? 70,
                Logs = logs,
                ChartDatesJson = JsonSerializer.Serialize(dates),
                ChartWeightsJson = JsonSerializer.Serialize(weights)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WeightTracker(WeightTrackingViewModel model)
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound();

            if (model.NewWeightKg <= 0)
            {
                ModelState.AddModelError("NewWeightKg", "Please enter a valid weight.");
            }

            if (!ModelState.IsValid)
            {
                var logs = member.WeightLogs.OrderByDescending(w => w.LoggedAt).ToList();
                var chronological = logs.OrderBy(w => w.LoggedAt).ToList();
                model.Logs = logs;
                model.ChartDatesJson = JsonSerializer.Serialize(chronological.Select(w => w.LoggedAt.ToString("MMM dd, yyyy")).ToArray());
                model.ChartWeightsJson = JsonSerializer.Serialize(chronological.Select(w => w.WeightKg).ToArray());
                return View(model);
            }

            decimal height = member.HeightCm ?? 170;
            decimal bmi = _bmiService.CalculateBmi(model.NewWeightKg, height);
            string category = _bmiService.GetBmiCategory(bmi);

            var log = new WeightLog
            {
                MemberId = member.MemberId,
                WeightKg = model.NewWeightKg,
                Bmi = bmi,
                Bmicategory = category,
                LoggedAt = DateTime.UtcNow
            };

            _context.WeightLogs.Add(log);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Weight logged successfully! New BMI: {bmi} ({category})";
            return RedirectToAction(nameof(WeightTracker));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestTrainer()
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

            return RedirectToAction(nameof(Profile));
        }
    }
}
