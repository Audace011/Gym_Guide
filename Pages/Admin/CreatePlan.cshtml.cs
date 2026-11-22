using System.Threading.Tasks;
using GymGuide.Data;
using GymGuide.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CreatePlanModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreatePlanModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MembershipPlan Plan { get; set; } = new()
        {
            DurationMonths = 1,
            Price = 49.99m,
            IncludesTrainer = false
        };

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.MembershipPlans.Add(Plan);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Membership Plan created successfully!";
            return RedirectToPage("/Admin/Plans");
        }
    }
}
