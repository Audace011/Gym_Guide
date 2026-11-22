using GymGuide.Data;
using GymGuide.Services;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITrainerAssignmentService _trainerAssignmentService;

        public IndexModel(ApplicationDbContext context, ITrainerAssignmentService trainerAssignmentService)
        {
            _context = context;
            _trainerAssignmentService = trainerAssignmentService;
        }

        public AdminDashboardViewModel Dashboard { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var totalMembers = await _context.Members.CountAsync();
            var totalTrainers = await _context.Trainers.CountAsync();
            var totalCourses = await _context.TrainingCourses.CountAsync();
            var paymentAmounts = await _context.Payments.Select(p => p.Amount).ToListAsync();
            var totalRevenue = paymentAmounts.Sum();

            var trainers = await _context.Trainers
                .Include(t => t.User)
                .Include(t => t.Members)
                .ToListAsync();

            var workloads = trainers.Select(t => new TrainerWorkloadReportItem
            {
                TrainerId = t.TrainerId,
                TrainerName = t.User?.FullName ?? "Unknown",
                Specialization = t.Specialization,
                CurrentClients = t.Members.Count,
                MaxClients = t.MaxClients,
                IsAvailable = t.IsAvailable
            }).ToList();

            var recentPayments = await _context.Payments
                .Include(p => p.Member)
                    .ThenInclude(m => m.User)
                .Include(p => p.Plan)
                .OrderByDescending(p => p.PaidAt)
                .Take(5)
                .ToListAsync();

            var pendingRequests = await _context.TrainerRequests
                .Include(r => r.Member)
                    .ThenInclude(m => m.User)
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            Dashboard = new AdminDashboardViewModel
            {
                TotalMembers = totalMembers,
                TotalTrainers = totalTrainers,
                TotalCourses = totalCourses,
                TotalRevenue = totalRevenue,
                TrainerWorkloads = workloads,
                RecentPayments = recentPayments,
                PendingRequests = pendingRequests
            };

            return Page();
        }

        public async Task<IActionResult> OnPostResolvePendingRequestAsync(int requestId)
        {
            var req = await _context.TrainerRequests.FindAsync(requestId);
            if (req != null)
            {
                var result = await _trainerAssignmentService.AssignTrainerAsync(req.MemberId, requestId);
                TempData["SuccessMessage"] = result.Message;
            }
            return RedirectToPage("/Admin/Index");
        }
    }
}
