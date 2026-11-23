
using GymGuide.Data;
using GymGuide.Services;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GymGuide.Controllers;

[Authorize(Roles = "Member")]
public class MemberController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly BmiService _bmiService;

    public MemberController(
        ApplicationDbContext context,
        BmiService bmiService)
    {
        _context = context;
        _bmiService = bmiService;
    }

    public async Task<IActionResult> Profile()
    {
        var member = await GetCurrentMemberAsync();
        if (member == null)
            return NotFound("Member profile not found.");

        var latestWeight = await _context.WeightLogs
            .Where(w => w.MemberId == member.MemberId)
            .OrderByDescending(w => w.LoggedAt)
            .FirstOrDefaultAsync();

        var activeCourse = await _context.MemberCourses
            .Where(mc => mc.MemberId == member.MemberId && mc.Status == "Active")
            .OrderByDescending(mc => mc.StartDate)
            .Select(mc => mc.Course.Name)
            .FirstOrDefaultAsync();

        var model = new MemberProfileViewModel
        {
            DateOfBirth = member.DateOfBirth,
            Gender = member.Gender,
            HeightCm = member.HeightCm,
            Goal = member.Goal,
            WeightKg = latestWeight?.WeightKg,
            RecommendedCourse = activeCourse
        };

        if (model.HeightCm.HasValue && model.WeightKg.HasValue)
        {
            var bmi = _bmiService.CalculateBmi(
                model.WeightKg.Value,
                model.HeightCm.Value);

            model.Bmi = bmi;
            model.BmiCategory = _bmiService.GetBmiCategory(bmi);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProfile(MemberProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Profile", model);

        var member = await GetCurrentMemberAsync();
        if (member == null)
            return NotFound("Member profile not found.");

        member.DateOfBirth = model.DateOfBirth;
        member.Gender = model.Gender;
        member.HeightCm = model.HeightCm;
        member.Goal = model.Goal;

        var bmi = _bmiService.CalculateBmi(
            model.WeightKg!.Value,
            model.HeightCm!.Value);
        var category = _bmiService.GetBmiCategory(bmi);

        var latestWeight = await _context.WeightLogs
            .Where(w => w.MemberId == member.MemberId)
            .OrderByDescending(w => w.LoggedAt)
            .FirstOrDefaultAsync();

        if (latestWeight == null || latestWeight.WeightKg != model.WeightKg.Value)
        {
            _context.WeightLogs.Add(new GymGuide.Models.WeightLog
            {
                MemberId = member.MemberId,
                WeightKg = model.WeightKg.Value,
                Bmi = bmi,
                Bmicategory = category,
                LoggedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "Your profile has been saved successfully!";

        return RedirectToAction("Profile");
    }

    public async Task<IActionResult> Progress()
    {
        var member = await GetCurrentMemberAsync();
        if (member == null)
            return NotFound("Member profile not found.");

        return View(await BuildProgressViewModelAsync(member));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordWeight(MemberProgressViewModel model)
    {
        var member = await GetCurrentMemberAsync();
        if (member == null)
            return NotFound("Member profile not found.");

        if (!ModelState.IsValid)
            return View("Progress", await BuildProgressViewModelAsync(member, model.WeightKg));

        if (member.HeightCm is not > 0)
        {
            ModelState.AddModelError(
                nameof(model.WeightKg),
                "Add a valid height to your profile before recording weight.");
            return View("Progress", await BuildProgressViewModelAsync(member, model.WeightKg));
        }

        var bmi = _bmiService.CalculateBmi(
            model.WeightKg!.Value,
            member.HeightCm.Value);
        var category = _bmiService.GetBmiCategory(bmi);

        _context.WeightLogs.Add(new GymGuide.Models.WeightLog
        {
            MemberId = member.MemberId,
            WeightKg = model.WeightKg.Value,
            Bmi = bmi,
            Bmicategory = category,
            LoggedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = "Your weight measurement has been recorded.";
        return RedirectToAction(nameof(Progress));
    }

    private async Task<GymGuide.Models.Member?> GetCurrentMemberAsync()
    {
        var identityUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (identityUserId == null)
            return null;

        return await _context.Members
            .Include(member => member.User)
            .FirstOrDefaultAsync(member => member.User.IdentityUserId == identityUserId);
    }

    private async Task<MemberProgressViewModel> BuildProgressViewModelAsync(
        GymGuide.Models.Member member,
        decimal? weightKg = null)
    {
        var measurements = await _context.WeightLogs
            .Where(weightLog => weightLog.MemberId == member.MemberId)
            .OrderByDescending(weightLog => weightLog.LoggedAt)
            .Select(weightLog => new WeightHistoryEntryViewModel
            {
                WeightKg = weightLog.WeightKg,
                Bmi = weightLog.Bmi,
                BmiCategory = weightLog.Bmicategory,
                LoggedAt = weightLog.LoggedAt
            })
            .ToListAsync();

        return new MemberProgressViewModel
        {
            HeightCm = member.HeightCm,
            WeightKg = weightKg,
            Measurements = measurements
        };
    }
}
