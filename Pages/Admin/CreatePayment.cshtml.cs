using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymGuide.Data;
using GymGuide.Models;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CreatePaymentModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreatePaymentModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public PaymentCreateViewModel Input { get; set; } = new();

        public SelectList MemberList { get; set; } = null!;
        public SelectList PlanList { get; set; } = null!;
        public Dictionary<int, decimal> PlanPrices { get; set; } = new();

        private async Task LoadSelectListsAsync()
        {
            var members = await _context.Members.Include(m => m.User).ToListAsync();
            var plans = await _context.MembershipPlans.ToListAsync();

            MemberList = new SelectList(members.Select(m => new { m.MemberId, Display = $"{m.User?.FullName} ({m.User?.Email})" }), "MemberId", "Display");
            PlanList = new SelectList(plans.Select(p => new { p.PlanId, Display = $"{p.Name} (${p.Price})" }), "PlanId", "Display");
            PlanPrices = plans.ToDictionary(p => p.PlanId, p => p.Price);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSelectListsAsync();
            var plans = await _context.MembershipPlans.ToListAsync();
            Input.Amount = plans.FirstOrDefault()?.Price ?? 30.00m;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync();
                return Page();
            }

            var payment = new Payment
            {
                MemberId = Input.MemberId,
                PlanId = Input.PlanId,
                Amount = Input.Amount,
                Method = Input.Method,
                PaidAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment recorded successfully!";
            return RedirectToPage("/Admin/Payments");
        }
    }
}
