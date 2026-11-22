using GymGuide.Data;
using GymGuide.Models;
using GymGuide.Services;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IBmiService _bmiService;
        private readonly IRecommendationService _recommendationService;

        public AccountController(
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

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            var identityUser = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(identityUser, model.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return View(model);
            }

            // Assign "Member" role
            await _userManager.AddToRoleAsync(identityUser, "Member");

            // Create Application User entity
            var userEntity = new User
            {
                IdentityUserId = identityUser.Id,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };
            _context.Users.Add(userEntity);
            await _context.SaveChangesAsync();

            // Create Member entity
            var memberEntity = new Member
            {
                UserId = userEntity.UserId,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                HeightCm = model.HeightCm,
                Goal = model.Goal
            };
            _context.Members.Add(memberEntity);
            await _context.SaveChangesAsync();

            // Calculate Initial BMI and create first WeightLog
            decimal bmi = _bmiService.CalculateBmi(model.InitialWeightKg, model.HeightCm);
            string bmiCategory = _bmiService.GetBmiCategory(bmi);

            var weightLog = new WeightLog
            {
                MemberId = memberEntity.MemberId,
                WeightKg = model.InitialWeightKg,
                Bmi = bmi,
                Bmicategory = bmiCategory,
                LoggedAt = DateTime.UtcNow
            };
            _context.WeightLogs.Add(weightLog);
            await _context.SaveChangesAsync();

            // Automatically recommend and assign first course
            try
            {
                await _recommendationService.AssignRecommendedCourseAsync(memberEntity.MemberId);
            }
            catch
            {
                // Non-critical if no courses exist yet
            }

            // Sign in member
            await _signInManager.SignInAsync(identityUser, isPersistent: false);
            TempData["SuccessMessage"] = "Welcome to GymGuide! Your profile and workout recommendation have been set up.";
            return RedirectToAction("MyCourse", "Course");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Admin"))
                        return RedirectToAction("Index", "Admin");
                    if (roles.Contains("Trainer"))
                        return RedirectToAction("Index", "Trainer");
                    if (roles.Contains("Member"))
                        return RedirectToAction("MyCourse", "Course");
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login credentials. Please check your email and password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
