namespace MediFlowAI.Api;

public static class DatabaseMigrator
{
    private static readonly string[] SpecializationNames =
    [
        "Cardiology",
        "Dermatology",
        "Pediatrics",
        "Orthopedics",
        "Neurology",
        "General Practice"
    ];

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        IServiceProvider serviceProvider = scope.ServiceProvider;
        AppDbContext dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting database migration...");

        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            await dbContext.Database.MigrateAsync();

            bool seeded = await SeedDatabaseAsync(dbContext, logger);

            stopwatch.Stop();
            logger.LogInformation(
                "Database migration completed successfully in {ElapsedMilliseconds} ms{SeedInfo}",
                stopwatch.ElapsedMilliseconds,
                seeded ? " (seed data applied)" : string.Empty);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "Database migration failed after {ElapsedMilliseconds} ms",
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    private static async Task<bool> SeedDatabaseAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Doctors.AnyAsync())
        {
            logger.LogInformation("Database already contains data. Skipping seed.");
            return false;
        }

        logger.LogInformation("Seeding database with dummy data...");
        Faker faker = new(locale: "en");

        List<Specialization> specializations = SpecializationNames
            .Select(name => new Specialization
            {
                Name = name
            })
            .ToList();

        List<Doctor> doctors = [];
        foreach (Specialization specialization in specializations)
        {
            int doctorCount = faker.Random.Int(2, 3);
            for (int i = 0; i < doctorCount; i++)
            {
                doctors.Add(new Doctor
                {
                    Name = $"Dr. {faker.Name.FullName()}",
                    SpecializationId = specialization.Id,
                    ConsultationFee = faker.Random.Decimal(80, 350),
                    IsActive = faker.Random.Bool(0.92f),
                    AverageRate = 0,
                    TotalReviews = 0
                });
            }
        }

        List<Patient> patients = new Faker<Patient>("en")
            .RuleFor(p => p.Id, _ => Guid.CreateVersion7())
            .RuleFor(p => p.FullName, f => f.Name.FullName())
            .RuleFor(p => p.PhoneNumber, f => f.Phone.PhoneNumber("01#########"))
            .RuleFor(p => p.BirthDate, f => f.Date.Past(60, DateTime.UtcNow.AddYears(-18)))
            .Generate(25);

        List<DoctorSchedule> schedules = GenerateSchedules(doctors, faker);

        List<Appointment> appointments = [];
        List<Review> reviews = [];

        DateTime now = DateTime.UtcNow;

        foreach (DoctorSchedule schedule in schedules.OrderBy(s => s.StartTime))
        {
            if (!faker.Random.Bool(0.35f))
            {
                continue;
            }

            Patient patient = faker.PickRandom(patients);
            AppointmentStatus status = schedule.StartTime < now
                ? AppointmentStatus.Completed
                : faker.PickRandom(AppointmentStatus.Pending, AppointmentStatus.Confirmed);

            Appointment appointment = new()
            {
                DoctorId = schedule.DoctorId,
                PatientId = patient.Id,
                DoctorScheduleId = schedule.Id,
                Status = status,
                CreatedAt = faker.Date.Between(schedule.StartTime.AddDays(-14), now)
            };

            appointments.Add(appointment);

            if (status == AppointmentStatus.Completed && faker.Random.Bool(0.75f))
            {
                int rate = faker.Random.Int(3, 5);
                reviews.Add(new Review
                {
                    DoctorId = schedule.DoctorId,
                    PatientId = patient.Id,
                    AppointmentId = appointment.Id,
                    Rate = rate,
                    Comment = faker.Random.Bool(0.7f) ? faker.Lorem.Sentence(faker.Random.Int(4, 12)) : null,
                    CreatedAt = schedule.StartTime.AddHours(faker.Random.Int(1, 48))
                });
            }
        }

        foreach (Doctor doctor in doctors)
        {
            List<Review> doctorReviews = reviews.Where(r => r.DoctorId == doctor.Id).ToList();
            doctor.TotalReviews = doctorReviews.Count;
            doctor.AverageRate = doctorReviews.Count > 0
                ? Math.Round(doctorReviews.Average(r => r.Rate), 2)
                : 0;
        }

        await db.Specializations.AddRangeAsync(specializations);
        await db.Doctors.AddRangeAsync(doctors);
        await db.Patients.AddRangeAsync(patients);
        await db.DoctorSchedules.AddRangeAsync(schedules);
        await db.Appointments.AddRangeAsync(appointments);
        await db.Reviews.AddRangeAsync(reviews);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Seed complete: {Specializations} specializations, {Doctors} doctors, {Patients} patients, {Schedules} slots ({Booked} booked), {Reviews} reviews",
            specializations.Count,
            doctors.Count,
            patients.Count,
            schedules.Count,
            appointments.Count,
            reviews.Count);

        return true;
    }

    private static List<DoctorSchedule> GenerateSchedules(List<Doctor> doctors, Faker faker)
    {
        List<DoctorSchedule> schedules = [];
        DateTime startDate = DateTime.UtcNow.Date;

        foreach (Doctor doctor in doctors.Where(d => d.IsActive))
        {
            int slotsForDoctor = faker.Random.Int(18, 28);

            for (int i = 0; i < slotsForDoctor; i++)
            {
                int dayOffset = faker.Random.Int(1, 10);
                DateTime day = startDate.AddDays(dayOffset);

                if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    day = day.AddDays(day.DayOfWeek == DayOfWeek.Saturday ? 2 : 1);
                }

                int hour = faker.Random.Int(9, 16);
                int minute = faker.PickRandom(0, 30);
                DateTime slotStart = day.AddHours(hour).AddMinutes(minute);
                DateTime slotEnd = slotStart.AddMinutes(30);

                schedules.Add(new DoctorSchedule
                {
                    DoctorId = doctor.Id,
                    StartTime = slotStart,
                    EndTime = slotEnd
                });
            }
        }

        return schedules
            .GroupBy(s => new { s.DoctorId, s.StartTime })
            .Select(g => g.First())
            .OrderBy(s => s.StartTime)
            .ToList();
    }
}