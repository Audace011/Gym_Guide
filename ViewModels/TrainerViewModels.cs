using GymGuide.Models;

namespace GymGuide.ViewModels
{
    public class TrainerDashboardViewModel
    {
        public Trainer Trainer { get; set; } = null!;
        public List<TrainerMemberItemViewModel> AssignedMembers { get; set; } = new List<TrainerMemberItemViewModel>();
    }

    public class TrainerMemberItemViewModel
    {
        public int MemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Goal { get; set; }
        public decimal LatestWeightKg { get; set; }
        public decimal LatestBmi { get; set; }
        public string LatestBmiCategory { get; set; } = string.Empty;
        public string ActiveCourseName { get; set; } = "None";
        public int CompletedSessionsCount { get; set; }
    }

    public class TrainerMemberDetailViewModel
    {
        public Member Member { get; set; } = null!;
        public List<WeightLog> WeightHistory { get; set; } = new List<WeightLog>();
        public MemberCourse? ActiveCourse { get; set; }
        public List<TrainingCourse> AvailableCourses { get; set; } = new List<TrainingCourse>();
        public int CompletedSessionsCount { get; set; }
    }
}
