using GymGuide.Data;
using GymGuide.Models;
using GymGuide.Services;
using GymGuide.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITrainerAssignmentService _trainerAssignmentService;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(
            ApplicationDbContext context, 
            ITrainerAssignmentService trainerAssignmentService,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _trainerAssignmentService = trainerAssignmentService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
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

            var vm = new AdminDashboardViewModel
            {
                TotalMembers = totalMembers,
                TotalTrainers = totalTrainers,
                TotalCourses = totalCourses,
                TotalRevenue = totalRevenue,
                TrainerWorkloads = workloads,
                RecentPayments = recentPayments,
                PendingRequests = pendingRequests
            };

            return View(vm);
        }

        // ==================== COURSE MANAGEMENT ====================
        [HttpGet]
        public async Task<IActionResult> Courses()
        {
            var courses = await _context.TrainingCourses
                .Include(c => c.CourseExercises)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(courses);
        }

        [HttpGet]
        public IActionResult CreateCourse()
        {
            return View(new TrainingCourse { Level = "Beginner", DurationWeeks = 8 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(TrainingCourse course)
        {
            if (!ModelState.IsValid) return View(course);

            _context.TrainingCourses.Add(course);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Course created successfully!";
            return RedirectToAction(nameof(Courses));
        }

        [HttpGet]
        public async Task<IActionResult> EditCourse(int id)
        {
            var course = await _context.TrainingCourses.FindAsync(id);
            if (course == null) return NotFound();
            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(TrainingCourse course)
        {
            if (!ModelState.IsValid) return View(course);

            _context.TrainingCourses.Update(course);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Course updated successfully!";
            return RedirectToAction(nameof(Courses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.TrainingCourses
                .Include(c => c.CourseExercises)
                .Include(c => c.MemberCourses)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course != null)
            {
                _context.CourseExercises.RemoveRange(course.CourseExercises);
                _context.MemberCourses.RemoveRange(course.MemberCourses);
                _context.TrainingCourses.Remove(course);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Course deleted successfully!";
            }
            return RedirectToAction(nameof(Courses));
        }

        // ==================== EXERCISE MANAGEMENT ====================
        [HttpGet]
        public async Task<IActionResult> Exercises()
        {
            var exercises = await _context.Exercises.OrderBy(e => e.Name).ToListAsync();
            return View(exercises);
        }

        [HttpGet]
        public IActionResult CreateExercise()
        {
            return View(new Exercise());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExercise(Exercise exercise)
        {
            if (!ModelState.IsValid) return View(exercise);

            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Exercise added successfully!";
            return RedirectToAction(nameof(Exercises));
        }

        [HttpGet]
        public async Task<IActionResult> EditExercise(int id)
        {
            var exercise = await _context.Exercises.FindAsync(id);
            if (exercise == null) return NotFound();
            return View(exercise);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExercise(Exercise exercise)
        {
            if (!ModelState.IsValid) return View(exercise);

            _context.Exercises.Update(exercise);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Exercise updated successfully!";
            return RedirectToAction(nameof(Exercises));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExercise(int id)
        {
            var exercise = await _context.Exercises
                .Include(e => e.CourseExercises)
                .FirstOrDefaultAsync(e => e.ExerciseId == id);

            if (exercise != null)
            {
                _context.CourseExercises.RemoveRange(exercise.CourseExercises);
                _context.Exercises.Remove(exercise);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Exercise deleted successfully!";
            }
            return RedirectToAction(nameof(Exercises));
        }

        // ==================== TRAINER MANAGEMENT ====================
        [HttpGet]
        public async Task<IActionResult> Trainers()
        {
            var trainers = await _context.Trainers
                .Include(t => t.User)
                .Include(t => t.Members)
                .ToListAsync();
            return View(trainers);
        }

        [HttpGet]
        public IActionResult CreateTrainer()
        {
            return View(new TrainerCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTrainer(TrainerCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingIdentity = await _userManager.FindByEmailAsync(model.Email);
            if (existingIdentity != null)
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
                foreach (var err in result.Errors) ModelState.AddModelError(string.Empty, err.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(identityUser, "Trainer");

            var userEntity = new User
            {
                IdentityUserId = identityUser.Id,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };
            _context.Users.Add(userEntity);
            await _context.SaveChangesAsync();

            var trainer = new Trainer
            {
                UserId = userEntity.UserId,
                Specialization = model.Specialization,
                MaxClients = model.MaxClients,
                IsAvailable = model.IsAvailable
            };
            _context.Trainers.Add(trainer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Trainer {model.FullName} added successfully!";
            return RedirectToAction(nameof(Trainers));
        }

        [HttpGet]
        public async Task<IActionResult> EditTrainer(int id)
        {
            var trainer = await _context.Trainers.Include(t => t.User).FirstOrDefaultAsync(t => t.TrainerId == id);
            if (trainer == null) return NotFound();

            var vm = new TrainerEditViewModel
            {
                TrainerId = trainer.TrainerId,
                FullName = trainer.User.FullName,
                Email = trainer.User.Email,
                PhoneNumber = trainer.User.PhoneNumber,
                Specialization = trainer.Specialization,
                MaxClients = trainer.MaxClients,
                IsAvailable = trainer.IsAvailable
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTrainer(TrainerEditViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var trainer = await _context.Trainers.Include(t => t.User).FirstOrDefaultAsync(t => t.TrainerId == model.TrainerId);
            if (trainer == null) return NotFound();

            // Update User details (Name, Phone)
            trainer.User.FullName = model.FullName;
            trainer.User.PhoneNumber = model.PhoneNumber;

            // Update Trainer details (Specialization, MaxClients, Availability)
            trainer.Specialization = model.Specialization;
            trainer.MaxClients = model.MaxClients;
            trainer.IsAvailable = model.IsAvailable;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Trainer details for {model.FullName} updated successfully!";
            return RedirectToAction(nameof(Trainers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTrainer(int id)
        {
            var trainer = await _context.Trainers
                .Include(t => t.Members)
                .Include(t => t.TrainerRequests)
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TrainerId == id);

            if (trainer != null)
            {
                // Unassign any members first
                foreach (var m in trainer.Members)
                {
                    m.TrainerId = null;
                }

                _context.TrainerRequests.RemoveRange(trainer.TrainerRequests);
                _context.Trainers.Remove(trainer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Trainer record removed successfully!";
            }

            return RedirectToAction(nameof(Trainers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTrainerAvailability(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer != null)
            {
                trainer.IsAvailable = !trainer.IsAvailable;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Trainer availability updated to {(trainer.IsAvailable ? "Available" : "Unavailable")}.";
            }
            return RedirectToAction(nameof(Trainers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTrainerCapacity(int id, int maxClients, string specialization)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer != null)
            {
                trainer.MaxClients = maxClients;
                trainer.Specialization = specialization;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Trainer capacity updated successfully!";
            }
            return RedirectToAction(nameof(Trainers));
        }

        // ==================== MEMBERSHIP PLANS ====================
        [HttpGet]
        public async Task<IActionResult> Plans()
        {
            var plans = await _context.MembershipPlans.ToListAsync();
            return View(plans);
        }

        [HttpGet]
        public IActionResult CreatePlan()
        {
            return View(new MembershipPlan());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePlan(MembershipPlan plan)
        {
            if (!ModelState.IsValid) return View(plan);
            _context.MembershipPlans.Add(plan);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Membership Plan created successfully!";
            return RedirectToAction(nameof(Plans));
        }

        // ==================== PAYMENTS (FR11) ====================
        [HttpGet]
        public async Task<IActionResult> Payments()
        {
            var payments = await _context.Payments
                .Include(p => p.Member)
                    .ThenInclude(m => m.User)
                .Include(p => p.Plan)
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();
            return View(payments);
        }

        [HttpGet]
        public async Task<IActionResult> CreatePayment()
        {
            var members = await _context.Members.Include(m => m.User).ToListAsync();
            var plans = await _context.MembershipPlans.ToListAsync();

            ViewBag.Members = new SelectList(members.Select(m => new { m.MemberId, Display = $"{m.User.FullName} ({m.User.Email})" }), "MemberId", "Display");
            ViewBag.Plans = new SelectList(plans.Select(p => new { p.PlanId, Display = $"{p.Name} (${p.Price})" }), "PlanId", "Display");
            ViewBag.PlanPrices = plans.ToDictionary(p => p.PlanId, p => p.Price);

            return View(new PaymentCreateViewModel { Amount = plans.FirstOrDefault()?.Price ?? 30.00m });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayment(PaymentCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var members = await _context.Members.Include(m => m.User).ToListAsync();
                var plans = await _context.MembershipPlans.ToListAsync();
                ViewBag.Members = new SelectList(members.Select(m => new { m.MemberId, Display = $"{m.User.FullName} ({m.User.Email})" }), "MemberId", "Display");
                ViewBag.Plans = new SelectList(plans.Select(p => new { p.PlanId, Display = $"{p.Name} (${p.Price})" }), "PlanId", "Display");
                ViewBag.PlanPrices = plans.ToDictionary(p => p.PlanId, p => p.Price);
                return View(model);
            }

            var payment = new Payment
            {
                MemberId = model.MemberId,
                PlanId = model.PlanId,
                Amount = model.Amount,
                Method = model.Method,
                PaidAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment recorded successfully!";
            return RedirectToAction(nameof(Payments));
        }

        // ==================== REPORTS (FR12) ====================
        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            // Active Members (members with active course or recent payment)
            var activeMembers = await _context.Members
                .Include(m => m.User)
                .Include(m => m.MemberCourses)
                    .ThenInclude(mc => mc.Course)
                .Include(m => m.Payments)
                    .ThenInclude(p => p.Plan)
                .Where(m => m.MemberCourses.Any(mc => mc.Status == "Active") || m.Payments.Any())
                .ToListAsync();

            ViewBag.ActiveMembers = activeMembers;

            // Trainer Workloads
            var trainers = await _context.Trainers
                .Include(t => t.User)
                .Include(t => t.Members)
                .ToListAsync();

            var workloads = trainers.Select(t => new TrainerWorkloadReportItem
            {
                TrainerId = t.TrainerId,
                TrainerName = t.User.FullName,
                Specialization = t.Specialization,
                CurrentClients = t.Members.Count,
                MaxClients = t.MaxClients,
                IsAvailable = t.IsAvailable
            }).ToList();

            ViewBag.TrainerWorkloads = workloads;

            // Revenue Report grouped by plan
            var plans = await _context.MembershipPlans.Include(p => p.Payments).ToListAsync();
            var revenuesByPlan = plans
                .Select(plan => new PlanRevenueItem
                {
                    PlanName = plan.Name,
                    TotalPaymentsCount = plan.Payments.Count,
                    TotalAmount = plan.Payments.Sum(p => p.Amount)
                }).ToList();

            var revenueVm = new RevenueReportViewModel
            {
                TotalRevenue = revenuesByPlan.Sum(p => p.TotalAmount),
                RevenuesByPlan = revenuesByPlan
            };

            return View(revenueVm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolvePendingRequest(int requestId)
        {
            var req = await _context.TrainerRequests.FindAsync(requestId);
            if (req != null)
            {
                var result = await _trainerAssignmentService.AssignTrainerAsync(req.MemberId, requestId);
                TempData["SuccessMessage"] = result.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // ==================== SYSTEM UNIT TESTS (SECTION 10 & 3.5 E) ====================
        [HttpGet]
        public async Task<IActionResult> RunTests()
        {
            var results = await GymGuide.Tests.UnitTestsRunner.RunAllTestsAsync(_context);
            return View(results);
        }
    }
}
