-- Seed Data for GymGuideDB
-- Run this script in SQL Server Management Studio (SSMS) or Azure Data Studio

USE [GymGuideDB];
GO

-- 1. Insert Membership Plans if not exist
IF NOT EXISTS (SELECT 1 FROM [MembershipPlans])
BEGIN
    INSERT INTO [MembershipPlans] ([Name], [DurationMonths], [Price], [IncludesTrainer])
    VALUES 
    (N'Basic 1-Month', 1, 30.00, 0),
    (N'Standard 3-Months', 3, 80.00, 0),
    (N'Premium 6-Months (With Personal Trainer)', 6, 180.00, 1);
END
GO

-- 2. Insert Exercises if not exist
IF NOT EXISTS (SELECT 1 FROM [Exercises])
BEGIN
    INSERT INTO [Exercises] ([Name], [MuscleGroup], [Description])
    VALUES 
    (N'Push-Ups', N'Chest', N'Classic bodyweight upper body exercise.'),
    (N'Incline Dumbbell Press', N'Chest', N'Builds upper chest strength and stability.'),
    (N'Lat Pulldown', N'Back', N'Strengthens latissimus dorsi and upper back.'),
    (N'Seated Cable Row', N'Back', N'Improves posture and middle back strength.'),
    (N'Bodyweight Squats', N'Legs', N'Foundational lower-body exercise for quads and glutes.'),
    (N'Romanian Deadlift', N'Legs', N'Targets hamstrings and lower back mechanics.'),
    (N'Plank Hold', N'Core', N'Isometric core stability exercise.'),
    (N'Treadmill Power Walk / Jog', N'Cardio', N'Fat-burning aerobic endurance workout.'),
    (N'Stationary Bike HIIT', N'Cardio', N'Low-impact high-intensity cardiovascular intervals.'),
    (N'Dumbbell Overhead Shoulder Press', N'Shoulders', N'Builds shoulder strength and vertical pushing power.');
END
GO

-- 3. Insert Training Courses if not exist
IF NOT EXISTS (SELECT 1 FROM [TrainingCourses])
BEGIN
    INSERT INTO [TrainingCourses] ([Name], [BMICategory], [Goal], [Level], [DurationWeeks], [Description])
    VALUES 
    (N'Beginner Weight Loss Accelerator', N'Overweight', N'Lose weight', N'Beginner', 8, N'Calorie-burning cardio and resistance training tailored to burn fat safely.'),
    (N'Gentle Movement & Fat Burn', N'Obese', N'Lose weight', N'Beginner', 12, N'Low-impact, joint-friendly cardio and mobility movements designed for steady weight reduction.'),
    (N'Lean Mass & Strength Foundations', N'Underweight', N'Build muscle', N'Beginner', 8, N'Progressive hypertrophy program combined with fundamental resistance training to build solid muscle.'),
    (N'Total Body Hypertrophy', N'Normal', N'Build muscle', N'Beginner', 8, N'Structured full-body splits targeting all major muscle groups for optimal muscle development.'),
    (N'Functional Fitness & Conditioning', N'Normal', N'Stay fit', N'Beginner', 6, N'Well-rounded program improving cardiovascular health, muscular endurance, and flexibility.'),
    (N'Active Health & Stamina', N'Overweight', N'Stay fit', N'Beginner', 8, N'Cardiovascular stamina and full body circuits to keep you energetic and healthy.'),
    (N'Vitality & Core Stability', N'Underweight', N'Stay fit', N'Beginner', 6, N'Gentle strength, core engagement, and functional movement to stay strong and healthy.'),
    (N'Solid Foundations Strength', N'Obese', N'Build muscle', N'Beginner', 10, N'Controlled resistance movements focusing on safe muscle activation and progressive overload.');
END
GO

-- 4. Link Exercises to Courses (CourseExercises) if not exist
IF NOT EXISTS (SELECT 1 FROM [CourseExercises])
BEGIN
    DECLARE @CourseId INT;
    DECLARE CourseCursor CURSOR FOR SELECT CourseId FROM [TrainingCourses];
    OPEN CourseCursor;
    FETCH NEXT FROM CourseCursor INTO @CourseId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Monday: Chest & Back
        INSERT INTO [CourseExercises] ([CourseId], [ExerciseId], [DayOfWeek], [Sets], [Reps], [DurationMinutes])
        SELECT @CourseId, ExerciseId, N'Monday', 3, 10, 15 FROM [Exercises] WHERE [Name] = N'Push-Ups';

        INSERT INTO [CourseExercises] ([CourseId], [ExerciseId], [DayOfWeek], [Sets], [Reps], [DurationMinutes])
        SELECT @CourseId, ExerciseId, N'Monday', 3, 12, 15 FROM [Exercises] WHERE [Name] = N'Lat Pulldown';

        -- Wednesday: Legs & Core
        INSERT INTO [CourseExercises] ([CourseId], [ExerciseId], [DayOfWeek], [Sets], [Reps], [DurationMinutes])
        SELECT @CourseId, ExerciseId, N'Wednesday', 3, 15, 20 FROM [Exercises] WHERE [Name] = N'Bodyweight Squats';

        INSERT INTO [CourseExercises] ([CourseId], [ExerciseId], [DayOfWeek], [Sets], [Reps], [DurationMinutes])
        SELECT @CourseId, ExerciseId, N'Wednesday', 3, 1, 10 FROM [Exercises] WHERE [Name] = N'Plank Hold';

        -- Friday: Cardio
        INSERT INTO [CourseExercises] ([CourseId], [ExerciseId], [DayOfWeek], [Sets], [Reps], [DurationMinutes])
        SELECT @CourseId, ExerciseId, N'Friday', 1, 1, 30 FROM [Exercises] WHERE [Name] = N'Treadmill Power Walk / Jog';

        FETCH NEXT FROM CourseCursor INTO @CourseId;
    END

    CLOSE CourseCursor;
    DEALLOCATE CourseCursor;
END
GO
