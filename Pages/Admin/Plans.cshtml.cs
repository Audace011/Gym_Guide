using System.Collections.Generic;
using System.Threading.Tasks;
using GymGuide.Data;
using GymGuide.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class PlansModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PlansModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<MembershipPlan> Plans { get; set; } = new();

        public async Task OnGetAsync()
        {
            Plans = await _context.MembershipPlans.ToListAsync();
        }
    }
}
