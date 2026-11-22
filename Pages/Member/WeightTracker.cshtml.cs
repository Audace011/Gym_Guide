using System.Text.Json;
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
    public class WeightTrackerModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IBmiService _bmiService;

        public WeightTrackerModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IBmiService bmiService)
        {
            _context = context;
            _userManager = userManager;
            _bmiService = bmiService;
        }

        [BindProperty]
        public WeightTrackingViewModel Input { get; set; } = new();

        private async Task<GymGuide.Models.Member?> GetCurrentMemberAsync()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return null;

            var userEntity = await _context.Users
                .Include(u => u.Member)
                    .ThenInclude(m => m!.WeightLogs)
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id || u.Email == identityUser.Email);

            return userEntity?.Member;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound();

            var logs = member.WeightLogs.OrderByDescending(w => w.LoggedAt).ToList();
            var latestLog = logs.FirstOrDefault();

            var chronological = logs.OrderBy(w => w.LoggedAt).ToList();
            var dates = chronological.Select(w => w.LoggedAt.ToString("MMM dd, yyyy")).ToArray();
            var weights = chronological.Select(w => w.WeightKg).ToArray();

            Input = new WeightTrackingViewModel
            {
                CurrentBmi = latestLog?.Bmi ?? 0,
                CurrentBmiCategory = latestLog?.Bmicategory ?? "N/A",
                NewWeightKg = latestLog?.WeightKg ?? 70,
                Logs = logs,
                ChartDatesJson = JsonSerializer.Serialize(dates),
                ChartWeightsJson = JsonSerializer.Serialize(weights)
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var member = await GetCurrentMemberAsync();
            if (member == null) return NotFound();

            if (Input.NewWeightKg <= 0)
            {
                ModelState.AddModelError("Input.NewWeightKg", "Please enter a valid weight.");
            }

            if (!ModelState.IsValid)
            {
                var logs = member.WeightLogs.OrderByDescending(w => w.LoggedAt).ToList();
                var chronological = logs.OrderBy(w => w.LoggedAt).ToList();
                Input.Logs = logs;
                Input.ChartDatesJson = JsonSerializer.Serialize(chronological.Select(w => w.LoggedAt.ToString("MMM dd, yyyy")).ToArray());
                Input.ChartWeightsJson = JsonSerializer.Serialize(chronological.Select(w => w.WeightKg).ToArray());
                return Page();
            }

            decimal height = member.HeightCm ?? 170;
            decimal bmi = _bmiService.CalculateBmi(height, Input.NewWeightKg);
            string category = _bmiService.GetBmiCategory(bmi);

            var log = new WeightLog
            {
                MemberId = member.MemberId,
                WeightKg = Input.NewWeightKg,
                Bmi = bmi,
                Bmicategory = category,
                LoggedAt = DateTime.UtcNow
            };

            _context.WeightLogs.Add(log);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Weight logged successfully! New BMI: {bmi} ({category})";
            return RedirectToPage("/Member/WeightTracker");
        }
    }
}
