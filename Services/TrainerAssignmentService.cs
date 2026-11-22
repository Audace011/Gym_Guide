using GymGuide.Data;
using GymGuide.Models;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Services
{
    public class TrainerAssignmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Trainer? AssignedTrainer { get; set; }
        public TrainerRequest Request { get; set; } = null!;
    }

    public interface ITrainerAssignmentService
    {
        Task<TrainerAssignmentResult> RequestTrainerAsync(int memberId);
        Task<TrainerAssignmentResult> AssignTrainerAsync(int memberId, int? specificRequestId = null);
        Task<bool> AdjustWorkoutPlanAsync(int trainerId, int memberId, int newCourseId);
    }

    public class TrainerAssignmentService : ITrainerAssignmentService
    {
        private readonly ApplicationDbContext _context;

        public TrainerAssignmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TrainerAssignmentResult> RequestTrainerAsync(int memberId)
        {
            var member = await _context.Members
                .Include(m => m.Trainer)
                .FirstOrDefaultAsync(m => m.MemberId == memberId);

            if (member == null)
            {
                return new TrainerAssignmentResult
                {
                    Success = false,
                    Message = "Member not found."
                };
            }

            // If already has an assigned trainer, report that
            if (member.TrainerId.HasValue)
            {
                var existingTrainer = await _context.Trainers
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.TrainerId == member.TrainerId.Value);

                return new TrainerAssignmentResult
                {
                    Success = true,
                    Message = $"You already have an assigned trainer: {existingTrainer?.User?.FullName}.",
                    AssignedTrainer = existingTrainer
                };
            }

            // Create initial request in Pending status
            var request = new TrainerRequest
            {
                MemberId = memberId,
                TrainerId = null,
                RequestedAt = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.TrainerRequests.Add(request);
            await _context.SaveChangesAsync();

            // Attempt automatic assignment
            return await AssignTrainerAsync(memberId, request.RequestId);
        }

        public async Task<TrainerAssignmentResult> AssignTrainerAsync(int memberId, int? specificRequestId = null)
        {
            var member = await _context.Members.FindAsync(memberId);
            if (member == null)
            {
                return new TrainerAssignmentResult { Success = false, Message = "Member not found." };
            }

            TrainerRequest? request = null;
            if (specificRequestId.HasValue)
            {
                request = await _context.TrainerRequests.FindAsync(specificRequestId.Value);
            }
            else
            {
                request = await _context.TrainerRequests
                    .Where(r => r.MemberId == memberId && r.Status == "Pending")
                    .OrderByDescending(r => r.RequestedAt)
                    .FirstOrDefaultAsync();
            }

            if (request == null)
            {
                request = new TrainerRequest
                {
                    MemberId = memberId,
                    RequestedAt = DateTime.UtcNow,
                    Status = "Pending"
                };
                _context.TrainerRequests.Add(request);
                await _context.SaveChangesAsync();
            }

            string memberGoal = member.Goal ?? "Stay fit";

            // 1. Find trainers where IsAvailable = true and Specialization matches member's goal
            var eligibleTrainers = await _context.Trainers
                .Include(t => t.User)
                .Include(t => t.Members)
                .Where(t => t.IsAvailable && t.Specialization == memberGoal)
                .ToListAsync();

            // 2. Filter out trainers who have reached MaxClients
            var availableTrainers = eligibleTrainers
                .Where(t => t.Members.Count < t.MaxClients)
                .ToList();

            // 3. Fallback: if no trainer with exact specialization has capacity, check other available trainers
            if (!availableTrainers.Any())
            {
                availableTrainers = await _context.Trainers
                    .Include(t => t.User)
                    .Include(t => t.Members)
                    .Where(t => t.IsAvailable && t.Members.Count < t.MaxClients)
                    .ToListAsync();
            }

            // 4. Select trainer with fewest current clients
            var chosenTrainer = availableTrainers
                .OrderBy(t => t.Members.Count)
                .ThenBy(t => t.TrainerId)
                .FirstOrDefault();

            if (chosenTrainer != null)
            {
                member.TrainerId = chosenTrainer.TrainerId;
                request.TrainerId = chosenTrainer.TrainerId;
                request.Status = "Assigned";

                await _context.SaveChangesAsync();

                return new TrainerAssignmentResult
                {
                    Success = true,
                    Message = $"Successfully assigned to trainer {chosenTrainer.User.FullName} ({chosenTrainer.Specialization}).",
                    AssignedTrainer = chosenTrainer,
                    Request = request
                };
            }
            else
            {
                // No trainer available -> request remains Pending
                request.Status = "Pending";
                await _context.SaveChangesAsync();

                return new TrainerAssignmentResult
                {
                    Success = false,
                    Message = "No trainer is currently available matching your criteria. Your request has been set to Pending and an administrator has been notified.",
                    Request = request
                };
            }
        }

        public async Task<bool> AdjustWorkoutPlanAsync(int trainerId, int memberId, int newCourseId)
        {
            var member = await _context.Members
                .Include(m => m.MemberCourses)
                .FirstOrDefaultAsync(m => m.MemberId == memberId);

            if (member == null)
                return false;

            // Verify trainer is assigned or admin
            if (member.TrainerId != trainerId)
            {
                // Verify trainer validity
                var trainer = await _context.Trainers.FindAsync(trainerId);
                if (trainer == null) return false;
            }

            // Mark previous active course as Replaced
            var activeCourses = member.MemberCourses.Where(mc => mc.Status == "Active").ToList();
            foreach (var active in activeCourses)
            {
                active.Status = "Replaced";
            }

            // Create new active course
            var newMemberCourse = new MemberCourse
            {
                MemberId = memberId,
                CourseId = newCourseId,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                Status = "Active"
            };

            _context.MemberCourses.Add(newMemberCourse);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
