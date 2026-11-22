using GymGuide.Data;
using GymGuide.Models;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CreateTrainerModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateTrainerModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public TrainerCreateViewModel Input { get; set; } = new();

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var existingIdentity = await _userManager.FindByEmailAsync(Input.Email);
            if (existingIdentity != null)
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
                foreach (var err in result.Errors) ModelState.AddModelError(string.Empty, err.Description);
                return Page();
            }

            await _userManager.AddToRoleAsync(identityUser, "Trainer");

            var userEntity = new User
            {
                IdentityUserId = identityUser.Id,
                FullName = Input.FullName,
                Email = Input.Email,
                PhoneNumber = Input.PhoneNumber
            };
            _context.Users.Add(userEntity);
            await _context.SaveChangesAsync();

            var trainer = new GymGuide.Models.Trainer
            {
                UserId = userEntity.UserId,
                Specialization = Input.Specialization,
                MaxClients = Input.MaxClients,
                IsAvailable = Input.IsAvailable
            };
            _context.Trainers.Add(trainer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Trainer {Input.FullName} added successfully!";
            return RedirectToPage("/Admin/Trainers");
        }
    }
}
