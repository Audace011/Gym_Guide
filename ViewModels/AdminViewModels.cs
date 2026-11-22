using GymGuide.Models;
using System.ComponentModel.DataAnnotations;

namespace GymGuide.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalMembers { get; set; }
        public int TotalTrainers { get; set; }
        public int TotalCourses { get; set; }
        public decimal TotalRevenue { get; set; }

        public List<TrainerWorkloadReportItem> TrainerWorkloads { get; set; } = new List<TrainerWorkloadReportItem>();
        public List<Payment> RecentPayments { get; set; } = new List<Payment>();
        public List<TrainerRequest> PendingRequests { get; set; } = new List<TrainerRequest>();
    }

    public class PaymentCreateViewModel
    {
        [Required(ErrorMessage = "Please select a member.")]
        [Display(Name = "Member")]
        public int MemberId { get; set; }

        [Required(ErrorMessage = "Please select a membership plan.")]
        [Display(Name = "Membership Plan")]
        public int PlanId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(1, 10000, ErrorMessage = "Enter valid payment amount.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        [Display(Name = "Payment Method")]
        public string Method { get; set; } = "Cash"; // Cash, Mobile Money, Card
    }

    public class TrainerWorkloadReportItem
    {
        public int TrainerId { get; set; }
        public string TrainerName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int CurrentClients { get; set; }
        public int MaxClients { get; set; }
        public bool IsAvailable { get; set; }
        public double WorkloadPercentage => MaxClients > 0 ? Math.Round((double)CurrentClients / MaxClients * 100, 1) : 0;
    }

    public class TrainerEditViewModel
    {
        public int TrainerId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required]
        public string Specialization { get; set; } = "Stay fit";

        [Required]
        [Range(1, 50, ErrorMessage = "Max clients must be between 1 and 50.")]
        [Display(Name = "Maximum Clients Capacity")]
        public int MaxClients { get; set; }

        [Display(Name = "Available for New Clients")]
        public bool IsAvailable { get; set; }
    }

    public class TrainerCreateViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "Trainer@12345";

        [Required]
        public string Specialization { get; set; } = "Stay fit";

        [Required]
        [Range(1, 50, ErrorMessage = "Max clients must be between 1 and 50.")]
        [Display(Name = "Maximum Clients Capacity")]
        public int MaxClients { get; set; } = 5;

        [Display(Name = "Is Available")]
        public bool IsAvailable { get; set; } = true;
    }

    public class RevenueReportViewModel
    {
        public decimal TotalRevenue { get; set; }
        public List<PlanRevenueItem> RevenuesByPlan { get; set; } = new List<PlanRevenueItem>();
    }

    public class PlanRevenueItem
    {
        public string PlanName { get; set; } = string.Empty;
        public int TotalPaymentsCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
