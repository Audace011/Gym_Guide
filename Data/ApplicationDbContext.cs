using System;
using System.Collections.Generic;
using GymGuide.Models;
using Microsoft.EntityFrameworkCore;

namespace GymGuide.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CourseExercise> CourseExercises { get; set; }

    public virtual DbSet<Exercise> Exercises { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<MemberCourse> MemberCourses { get; set; }

    public virtual DbSet<MembershipPlan> MembershipPlans { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<SessionLog> SessionLogs { get; set; }

    public virtual DbSet<Trainer> Trainers { get; set; }

    public virtual DbSet<TrainerRequest> TrainerRequests { get; set; }

    public virtual DbSet<TrainingCourse> TrainingCourses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<WeightLog> WeightLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CourseExercise>(entity =>
        {
            entity.HasKey(e => e.CourseExerciseId).HasName("PK__CourseEx__DF4C8BE1C3DB7DFC");

            entity.Property(e => e.DayOfWeek).HasMaxLength(10);

            entity.HasOne(d => d.Course).WithMany(p => p.CourseExercises)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CourseExe__Cours__5FB337D6");

            entity.HasOne(d => d.Exercise).WithMany(p => p.CourseExercises)
                .HasForeignKey(d => d.ExerciseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CourseExe__Exerc__60A75C0F");
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.HasKey(e => e.ExerciseId).HasName("PK__Exercise__A074AD2F9314F5A4");

            entity.Property(e => e.MuscleGroup).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.MemberId).HasName("PK__Members__0CF04B18A537D060");

            entity.HasIndex(e => e.UserId, "UQ__Members__1788CC4DC872D296").IsUnique();

            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Goal).HasMaxLength(30);
            entity.Property(e => e.HeightCm).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Trainer).WithMany(p => p.Members)
                .HasForeignKey(d => d.TrainerId)
                .HasConstraintName("FK__Members__Trainer__5535A963");

            entity.HasOne(d => d.User).WithOne(p => p.Member)
                .HasForeignKey<Member>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Members__UserId__5441852A");
        });

        modelBuilder.Entity<MemberCourse>(entity =>
        {
            entity.HasKey(e => e.MemberCourseId).HasName("PK__MemberCo__B47E18E78D90B7CD");

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Course).WithMany(p => p.MemberCourses)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MemberCou__Cours__6477ECF3");

            entity.HasOne(d => d.Member).WithMany(p => p.MemberCourses)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MemberCou__Membe__6383C8BA");
        });

        modelBuilder.Entity<MembershipPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId).HasName("PK__Membersh__755C22B7F6C4AC2F");

            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38AC9DEE4F");

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Method).HasMaxLength(30);
            entity.Property(e => e.PaidAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Member).WithMany(p => p.Payments)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__Member__74AE54BC");

            entity.HasOne(d => d.Plan).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__PlanId__75A278F5");
        });

        modelBuilder.Entity<SessionLog>(entity =>
        {
            entity.HasKey(e => e.SessionLogId).HasName("PK__SessionL__96CBDF0B1F0D2832");

            entity.HasOne(d => d.MemberCourse).WithMany(p => p.SessionLogs)
                .HasForeignKey(d => d.MemberCourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SessionLo__Membe__68487DD7");
        });

        modelBuilder.Entity<Trainer>(entity =>
        {
            entity.HasKey(e => e.TrainerId).HasName("PK__Trainers__366A1A7C1EC5E27A");

            entity.HasIndex(e => e.UserId, "UQ__Trainers__1788CC4D489395AA").IsUnique();

            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.MaxClients).HasDefaultValue(10);
            entity.Property(e => e.Specialization).HasMaxLength(50);

            entity.HasOne(d => d.User).WithOne(p => p.Trainer)
                .HasForeignKey<Trainer>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Trainers__UserId__4E88ABD4");
        });

        modelBuilder.Entity<TrainerRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__TrainerR__33A8517A1D92C8E6");

            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Member).WithMany(p => p.TrainerRequests)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TrainerRe__Membe__6C190EBB");

            entity.HasOne(d => d.Trainer).WithMany(p => p.TrainerRequests)
                .HasForeignKey(d => d.TrainerId)
                .HasConstraintName("FK__TrainerRe__Train__6D0D32F4");
        });

        modelBuilder.Entity<TrainingCourse>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Training__C92D71A779669B95");

            entity.Property(e => e.Bmicategory)
                .HasMaxLength(20)
                .HasColumnName("BMICategory");
            entity.Property(e => e.Goal).HasMaxLength(30);
            entity.Property(e => e.Level).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C30ED24F2");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534517C9D49").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IdentityUserId).HasMaxLength(450);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<WeightLog>(entity =>
        {
            entity.HasKey(e => e.WeightLogId).HasName("PK__WeightLo__3A1BED9572664D56");

            entity.Property(e => e.Bmi)
                .HasColumnType("decimal(4, 1)")
                .HasColumnName("BMI");
            entity.Property(e => e.Bmicategory)
                .HasMaxLength(20)
                .HasColumnName("BMICategory");
            entity.Property(e => e.LoggedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.WeightKg).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.Member).WithMany(p => p.WeightLogs)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__WeightLog__Membe__5812160E");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
