using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymGuide.Data;
using GymGuide.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class PaymentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PaymentsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Payment> Payments { get; set; } = new();

        public async Task OnGetAsync()
        {
            Payments = await _context.Payments
                .Include(p => p.Member)
                    .ThenInclude(m => m.User)
                .Include(p => p.Plan)
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();
        }
    }
}
