using GymGuide.Data;
using GymGuide.Models;
using GymGuide.Services;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GymGuide.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IBmiService _bmiService;
        private readonly IRecommendationService _recommendationService;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context,
            IBmiService bmiService,
            IRecommendationService recommendationService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _bmiService = bmiService;
            _recommendationService = recommendationService;
        }

        [BindProperty]
        public RegisterViewModel Input { get; set; } = new();

        public IActionResult OnGet()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Email", "An account with this email already exists.");
                return Page();
            }

            var identityUser = new IdentityUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                PhoneNumber = Input.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(identityUser, Input.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return Page();
            }

            await _userManager.AddToRoleAsync(identityUser, "Member");

            var userEntity = new User
            {
                IdentityUserId = identityUser.Id,
                FullName = Input.FullName,
                Email = Input.Email,
                PhoneNumber = Input.PhoneNumber
            };
            _context.Users.Add(userEntity);
            await _context.SaveChangesAsync();

            var memberEntity = new GymGuide.Models.Member
            {
                UserId = userEntity.UserId,
                DateOfBirth = Input.DateOfBirth,
                Gender = Input.Gender,
                HeightCm = Input.HeightCm,
                Goal = Input.Goal
            };
            _context.Members.Add(memberEntity);
            await _context.SaveChangesAsync();

            var bmi = _bmiService.CalculateBmi(Input.HeightCm, Input.InitialWeightKg);
            var category = _bmiService.GetBmiCategory(bmi);

            var weightLog = new WeightLog
            {
                MemberId = memberEntity.MemberId,
                WeightKg = Input.InitialWeightKg,
                Bmi = bmi,
                Bmicategory = category,
                LoggedAt = DateTime.UtcNow
            };
            _context.WeightLogs.Add(weightLog);
            await _context.SaveChangesAsync();

            try
            {
                await _recommendationService.AssignRecommendedCourseAsync(memberEntity.MemberId);
            }
            catch
            {
                // Non-critical if no courses exist yet
            }

            await _signInManager.SignInAsync(identityUser, isPersistent: false);
            TempData["SuccessMessage"] = "Welcome to GymGuide! Your profile and workout recommendation have been set up.";
            return RedirectToPage("/Course/MyCourse");
        }
    }
}
