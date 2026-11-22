using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymGuide.Data;
using GymGuide.Models;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReportsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public RevenueReportViewModel RevenueReport { get; set; } = new();
        public List<TrainerWorkloadReportItem> TrainerWorkloads { get; set; } = new();
        public List<GymGuide.Models.Member> ActiveMembers { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Active Members (members with active course or recent payment)
            ActiveMembers = await _context.Members
                .Include(m => m.User)
                .Include(m => m.MemberCourses)
                    .ThenInclude(mc => mc.Course)
                .Include(m => m.Payments)
                    .ThenInclude(p => p.Plan)
                .Where(m => m.MemberCourses.Any(mc => mc.Status == "Active") || m.Payments.Any())
                .ToListAsync();

            // Trainer Workloads
            var trainers = await _context.Trainers
                .Include(t => t.User)
                .Include(t => t.Members)
                .ToListAsync();

            TrainerWorkloads = trainers.Select(t => new TrainerWorkloadReportItem
            {
                TrainerId = t.TrainerId,
                TrainerName = t.User?.FullName ?? "Unknown",
                Specialization = t.Specialization,
                CurrentClients = t.Members.Count,
                MaxClients = t.MaxClients,
                IsAvailable = t.IsAvailable
            }).ToList();

            // Revenue Report grouped by plan (materialized in memory for SQLite compatibility)
            var plans = await _context.MembershipPlans.Include(p => p.Payments).ToListAsync();
            var revenuesByPlan = plans
                .Select(plan => new PlanRevenueItem
                {
                    PlanName = plan.Name,
                    TotalPaymentsCount = plan.Payments.Count,
                    TotalAmount = plan.Payments.Sum(p => p.Amount)
                }).ToList();

            RevenueReport = new RevenueReportViewModel
            {
                TotalRevenue = revenuesByPlan.Sum(p => p.TotalAmount),
                RevenuesByPlan = revenuesByPlan
            };
        }
    }
}
