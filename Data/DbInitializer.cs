using GymGuide.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace GymGuide.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var context = services.GetRequiredService<ApplicationDbContext>();
            var authContext = services.GetRequiredService<AuthDbContext>();

            // Ensure database is created by AuthContext
            await authContext.Database.EnsureCreatedAsync();

            // Ensure ApplicationDbContext tables are created in the existing database
            var databaseCreator = context.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            if (databaseCreator != null)
            {
                try
                {
                    await databaseCreator.CreateTablesAsync();
                }
                catch
                {
                    // Ignore if tables already exist
                }
            }

            // 1. Roles
            string[] roles = { "Member", "Trainer", "Admin" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Admin User
            const string adminEmail = "admin@gymguide.test";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, "Admin@12345");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");

                    if (!context.Users.Any(u => u.Email == adminEmail))
                    {
                        context.Users.Add(new User
                        {
                            IdentityUserId = adminUser.Id,
                            FullName = "System Administrator",
                            Email = adminEmail,
                            PhoneNumber = "+1234567800"
                        });
                        await context.SaveChangesAsync();
                    }
                }
            }

            // 3. Seed Trainers
            var seedTrainers = new[]
            {
                new { Email = "john.trainer@gymguide.test", Name = "John Smith", Specialization = "Build muscle", MaxClients = 5 },
                new { Email = "sarah.trainer@gymguide.test", Name = "Sarah Johnson", Specialization = "Lose weight", MaxClients = 5 },
                new { Email = "mike.trainer@gymguide.test", Name = "Mike Wilson", Specialization = "Stay fit", MaxClients = 5 }
            };

            foreach (var t in seedTrainers)
            {
                var identityTrainer = await userManager.FindByEmailAsync(t.Email);
                if (identityTrainer == null)
                {
                    identityTrainer = new IdentityUser
                    {
                        UserName = t.Email,
                        Email = t.Email,
                        EmailConfirmed = true
                    };
                    var res = await userManager.CreateAsync(identityTrainer, "Trainer@12345");
                    if (res.Succeeded)
                    {
                        await userManager.AddToRoleAsync(identityTrainer, "Trainer");
                    }
                }

                var existingUser = await context.Users.Include(u => u.Trainer).FirstOrDefaultAsync(u => u.Email == t.Email);
                if (existingUser == null)
                {
                    var userEntity = new User
                    {
                        IdentityUserId = identityTrainer.Id,
                        FullName = t.Name,
                        Email = t.Email,
                        PhoneNumber = "+1234567891"
                    };
                    context.Users.Add(userEntity);
                    await context.SaveChangesAsync();

                    context.Trainers.Add(new Trainer
                    {
                        UserId = userEntity.UserId,
                        Specialization = t.Specialization,
                        MaxClients = t.MaxClients,
                        IsAvailable = true
                    });
                    await context.SaveChangesAsync();
                }
            }

            // 4. Seed Membership Plans
            if (!await context.MembershipPlans.AnyAsync())
            {
                context.MembershipPlans.AddRange(
                    new MembershipPlan
                    {
                        Name = "Basic 1-Month",
                        DurationMonths = 1,
                        Price = 30.00m,
                        IncludesTrainer = false
                    },
                    new MembershipPlan
                    {
                        Name = "Standard 3-Months",
                        DurationMonths = 3,
                        Price = 80.00m,
                        IncludesTrainer = false
                    },
                    new MembershipPlan
                    {
                        Name = "Premium 6-Months (With Personal Trainer)",
                        DurationMonths = 6,
                        Price = 180.00m,
                        IncludesTrainer = true
                    }
                );
                await context.SaveChangesAsync();
            }

            // 5. Seed Exercises
            if (!await context.Exercises.AnyAsync())
            {
                context.Exercises.AddRange(
                    new Exercise { Name = "Push-Ups", MuscleGroup = "Chest", Description = "Classic bodyweight upper body exercise." },
                    new Exercise { Name = "Incline Dumbbell Press", MuscleGroup = "Chest", Description = "Builds upper chest strength and stability." },
                    new Exercise { Name = "Lat Pulldown", MuscleGroup = "Back", Description = "Strengthens latissimus dorsi and upper back." },
                    new Exercise { Name = "Seated Cable Row", MuscleGroup = "Back", Description = "Improves posture and middle back strength." },
                    new Exercise { Name = "Bodyweight Squats", MuscleGroup = "Legs", Description = "Foundational lower-body exercise for quads and glutes." },
                    new Exercise { Name = "Romanian Deadlift", MuscleGroup = "Legs", Description = "Targets hamstrings and lower back mechanics." },
                    new Exercise { Name = "Plank Hold", MuscleGroup = "Core", Description = "Isometric core stability exercise." },
                    new Exercise { Name = "Treadmill Power Walk / Jog", MuscleGroup = "Cardio", Description = "Fat-burning aerobic endurance workout." },
                    new Exercise { Name = "Stationary Bike HIIT", MuscleGroup = "Cardio", Description = "Low-impact high-intensity cardiovascular intervals." },
                    new Exercise { Name = "Dumbbell Overhead Shoulder Press", MuscleGroup = "Shoulders", Description = "Builds shoulder strength and vertical pushing power." }
                );
                await context.SaveChangesAsync();
            }

            // 6. Seed Training Courses
            if (!await context.TrainingCourses.AnyAsync())
            {
                var courses = new List<TrainingCourse>
                {
                    new TrainingCourse
                    {
                        Name = "Beginner Weight Loss Accelerator",
                        Bmicategory = "Overweight",
                        Goal = "Lose weight",
                        Level = "Beginner",
                        DurationWeeks = 8,
                        Description = "Calorie-burning cardio and resistance training tailored to burn fat safely."
                    },
                    new TrainingCourse
                    {
                        Name = "Gentle Movement & Fat Burn",
                        Bmicategory = "Obese",
                        Goal = "Lose weight",
                        Level = "Beginner",
                        DurationWeeks = 12,
                        Description = "Low-impact, joint-friendly cardio and mobility movements designed for steady weight reduction."
                    },
                    new TrainingCourse
                    {
                        Name = "Lean Mass & Strength Foundations",
                        Bmicategory = "Underweight",
                        Goal = "Build muscle",
                        Level = "Beginner",
                        DurationWeeks = 8,
                        Description = "Progressive hypertrophy program combined with fundamental resistance training to build solid muscle."
                    },
                    new TrainingCourse
                    {
                        Name = "Total Body Hypertrophy",
                        Bmicategory = "Normal",
                        Goal = "Build muscle",
                        Level = "Beginner",
                        DurationWeeks = 8,
                        Description = "Structured full-body splits targeting all major muscle groups for optimal muscle development."
                    },
                    new TrainingCourse
                    {
                        Name = "Functional Fitness & Conditioning",
                        Bmicategory = "Normal",
                        Goal = "Stay fit",
                        Level = "Beginner",
                        DurationWeeks = 6,
                        Description = "Well-rounded program improving cardiovascular health, muscular endurance, and flexibility."
                    },
                    new TrainingCourse
                    {
                        Name = "Active Health & Stamina",
                        Bmicategory = "Overweight",
                        Goal = "Stay fit",
                        Level = "Beginner",
                        DurationWeeks = 8,
                        Description = "Cardiovascular stamina and full body circuits to keep you energetic and healthy."
                    },
                    new TrainingCourse
                    {
                        Name = "Vitality & Core Stability",
                        Bmicategory = "Underweight",
                        Goal = "Stay fit",
                        Level = "Beginner",
                        DurationWeeks = 6,
                        Description = "Gentle strength, core engagement, and functional movement to stay strong and healthy."
                    },
                    new TrainingCourse
                    {
                        Name = "Solid Foundations Strength",
                        Bmicategory = "Obese",
                        Goal = "Build muscle",
                        Level = "Beginner",
                        DurationWeeks = 10,
                        Description = "Controlled resistance movements focusing on safe muscle activation and progressive overload."
                    }
                };

                context.TrainingCourses.AddRange(courses);
                await context.SaveChangesAsync();

                // Map Exercises to Courses
                var exList = await context.Exercises.ToListAsync();
                if (exList.Count >= 6)
                {
                    foreach (var c in courses)
                    {
                        context.CourseExercises.AddRange(
                            new CourseExercise
                            {
                                CourseId = c.CourseId,
                                ExerciseId = exList[0].ExerciseId,
                                DayOfWeek = "Monday",
                                Sets = 3,
                                Reps = 10,
                                DurationMinutes = 15
                            },
                            new CourseExercise
                            {
                                CourseId = c.CourseId,
                                ExerciseId = exList[2].ExerciseId,
                                DayOfWeek = "Monday",
                                Sets = 3,
                                Reps = 12,
                                DurationMinutes = 15
                            },
                            new CourseExercise
                            {
                                CourseId = c.CourseId,
                                ExerciseId = exList[4].ExerciseId,
                                DayOfWeek = "Wednesday",
                                Sets = 3,
                                Reps = 15,
                                DurationMinutes = 20
                            },
                            new CourseExercise
                            {
                                CourseId = c.CourseId,
                                ExerciseId = exList[6].ExerciseId,
                                DayOfWeek = "Wednesday",
                                Sets = 3,
                                Reps = 1,
                                DurationMinutes = 10
                            },
                            new CourseExercise
                            {
                                CourseId = c.CourseId,
                                ExerciseId = exList[7].ExerciseId,
                                DayOfWeek = "Friday",
                                Sets = 1,
                                Reps = 1,
                                DurationMinutes = 30
                            }
                        );
                    }
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
