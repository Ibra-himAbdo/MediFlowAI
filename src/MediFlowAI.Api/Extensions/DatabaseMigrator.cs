namespace MediFlowAI.Api;

public static class DatabaseMigrator
{
    private const int ScheduleDaysBefore = 3;
    private const int ScheduleDaysAfter = 10;
    private const int DoctorsPerSpecialization = 2;

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
            await EnsureDemoPatientAsync(dbContext, logger);
            await EnsureScheduleRangeAsync(dbContext, logger);

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
        DateTime utcNow = DateTime.UtcNow;

        List<Specialization> specializations = SpecializationNames
            .Select(name => new Specialization
            {
                Name = name
            })
            .ToList();

        List<Doctor> doctors = [];
        foreach (Specialization specialization in specializations)
        {
            for (int i = 0; i < DoctorsPerSpecialization; i++)
            {
                doctors.Add(new Doctor
                {
                    Name = $"Dr. {faker.Name.FullName()}",
                    SpecializationId = specialization.Id,
                    ConsultationFee = faker.Random.Decimal(80, 350),
                    IsActive = true,
                    AverageRate = 0,
                    TotalReviews = 0
                });
            }
        }

        List<Patient> patients =
        [
            new Patient
            {
                Id = DemoPatient.Id,
                FullName = DemoPatient.FullName,
                PhoneNumber = DemoPatient.PhoneNumber,
                BirthDate = DateTime.UtcNow.AddYears(-30)
            },
            .. new Faker<Patient>("en")
                .RuleFor(p => p.Id, _ => Guid.CreateVersion7())
                .RuleFor(p => p.FullName, f => f.Name.FullName())
                .RuleFor(p => p.PhoneNumber, f => f.Phone.PhoneNumber("01#########"))
                .RuleFor(p => p.BirthDate, f => f.Date.Past(60, DateTime.UtcNow.AddYears(-18)))
                .Generate(24)
        ];

        List<DoctorSchedule> schedules = CreateSchedulesForRange(doctors, utcNow);

        List<Appointment> appointments = [];
        List<Review> reviews = [];

        foreach (DoctorSchedule schedule in schedules.OrderBy(s => s.StartTime))
        {
            if (!faker.Random.Bool(0.35f))
            {
                continue;
            }

            Patient patient = faker.PickRandom(patients);
            AppointmentStatus status = schedule.StartTime < utcNow
                ? AppointmentStatus.Completed
                : faker.PickRandom(AppointmentStatus.Pending, AppointmentStatus.Confirmed);

            Appointment appointment = new()
            {
                DoctorId = schedule.DoctorId,
                PatientId = patient.Id,
                DoctorScheduleId = schedule.Id,
                Status = status,
                CreatedAt = faker.Date.Between(schedule.StartTime.AddDays(-14), utcNow)
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

        (DateTime rangeStart, DateTime rangeEnd) = GetScheduleRange(utcNow);
        logger.LogInformation(
            "Seed complete: {Specializations} specializations, {Doctors} doctors ({PerSpec} per specialty), {Patients} patients, {Schedules} slots ({Booked} booked), {Reviews} reviews. Schedule range: {From:yyyy-MM-dd} to {To:yyyy-MM-dd}",
            specializations.Count,
            doctors.Count,
            DoctorsPerSpecialization,
            patients.Count,
            schedules.Count,
            appointments.Count,
            reviews.Count,
            rangeStart,
            rangeEnd.AddDays(-1));

        return true;
    }

    /// <summary>
    /// Ensures the demo AI patient exists (for booking/reviews when UserId is omitted).
    /// </summary>
    private static async Task EnsureDemoPatientAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Patients.AnyAsync(p => p.Id == DemoPatient.Id))
        {
            return;
        }

        await db.Patients.AddAsync(new Patient
        {
            Id = DemoPatient.Id,
            FullName = DemoPatient.FullName,
            PhoneNumber = DemoPatient.PhoneNumber,
            BirthDate = DateTime.UtcNow.AddYears(-30)
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Ensured demo patient {PatientId} ({Name}).", DemoPatient.Id, DemoPatient.FullName);
    }

    /// <summary>
    /// Fills missing slots from 3 days ago through 10 days ahead (all specializations represented).
    /// </summary>
    private static async Task EnsureScheduleRangeAsync(AppDbContext db, ILogger logger)
    {
        List<Doctor> activeDoctors = await db.Doctors.AsNoTracking()
            .Where(d => d.IsActive)
            .ToListAsync();

        if (activeDoctors.Count == 0)
        {
            return;
        }

        DateTime utcNow = DateTime.UtcNow;
        (DateTime rangeStart, DateTime rangeEnd) = GetScheduleRange(utcNow);
        List<DoctorSchedule> desired = CreateSchedulesForRange(activeDoctors, utcNow);

        HashSet<(Guid DoctorId, DateTime StartTime)> existing = await db.DoctorSchedules
            .AsNoTracking()
            .Where(s => s.StartTime >= rangeStart && s.StartTime < rangeEnd)
            .Select(s => new ValueTuple<Guid, DateTime>(s.DoctorId, s.StartTime))
            .ToHashSetAsync();

        List<DoctorSchedule> toAdd = desired
            .Where(s => !existing.Contains((s.DoctorId, s.StartTime)))
            .ToList();

        if (toAdd.Count == 0)
        {
            return;
        }

        await db.DoctorSchedules.AddRangeAsync(toAdd);
        await db.SaveChangesAsync();
        logger.LogInformation(
            "Ensured {Count} schedule slots from {From:yyyy-MM-dd} to {To:yyyy-MM-dd}.",
            toAdd.Count,
            rangeStart,
            rangeEnd.AddDays(-1));
    }

    private static (DateTime Start, DateTime EndExclusive) GetScheduleRange(DateTime utcNow) =>
        (utcNow.Date.AddDays(-ScheduleDaysBefore), utcNow.Date.AddDays(ScheduleDaysAfter + 1));

    private static List<DoctorSchedule> CreateSchedulesForRange(IEnumerable<Doctor> doctors, DateTime utcNow)
    {
        // UTC hours: morning, afternoon, and evening (22 = 10 PM) for time-specific booking demos
        int[] slotHours = [9, 10, 11, 14, 15, 16, 18, 19, 20, 21, 22];
        (DateTime rangeStart, DateTime rangeEnd) = GetScheduleRange(utcNow);
        List<Doctor> activeDoctors = doctors.Where(d => d.IsActive).OrderBy(d => d.Id).ToList();
        List<DoctorSchedule> schedules = [];

        for (DateTime day = rangeStart; day < rangeEnd; day = day.AddDays(1))
        {
            foreach (Doctor doctor in GetDoctorsOnDutyForDay(activeDoctors, day))
            {
                foreach (int hour in slotHours)
                {
                    DateTime slotStart = DateTime.SpecifyKind(day.AddHours(hour), DateTimeKind.Utc);

                    schedules.Add(new DoctorSchedule
                    {
                        DoctorId = doctor.Id,
                        StartTime = slotStart,
                        EndTime = slotStart.AddMinutes(30)
                    });
                }
            }
        }

        return schedules
            .GroupBy(s => new { s.DoctorId, s.StartTime })
            .Select(g => g.First())
            .OrderBy(s => s.StartTime)
            .ToList();
    }

    /// <summary>
    /// Sun–Thu: all active doctors. Fri–Sat: one on-call doctor per specialization (easy to test every specialty).
    /// </summary>
    private static List<Doctor> GetDoctorsOnDutyForDay(List<Doctor> activeDoctors, DateTime day)
    {
        if (activeDoctors.Count == 0)
        {
            return [];
        }

        if (day.DayOfWeek is not (DayOfWeek.Friday or DayOfWeek.Saturday))
        {
            return activeDoctors;
        }

        return activeDoctors
            .GroupBy(d => d.SpecializationId)
            .Select(g => g.OrderBy(d => d.Id).First())
            .ToList();
    }
}
