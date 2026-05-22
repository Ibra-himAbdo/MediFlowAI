namespace MediFlowAI.Services;

public class AppointmentService(AppDbContext db) : IAppointmentService
{
    public async Task<BookingResultDto> BookAppointmentAsync(
        Guid patientId,
        Guid doctorScheduleId,
        CancellationToken cancellationToken = default)
    {
        bool patientExists = await db.Patients
            .AsNoTracking()
            .AnyAsync(p => p.Id == patientId, cancellationToken);

        if (!patientExists)
        {
            return new BookingResultDto(false, null, "Patient not found.");
        }

        DoctorSchedule? schedule = await db.DoctorSchedules
            .Include(s => s.Appointment)
            .FirstOrDefaultAsync(s => s.Id == doctorScheduleId, cancellationToken);

        if (schedule is null)
        {
            return new BookingResultDto(false, null, "Schedule slot not found.");
        }

        if (schedule.Appointment is not null)
        {
            return new BookingResultDto(false, null, "This slot is already booked.");
        }

        if (schedule.StartTime < DateTime.UtcNow)
        {
            return new BookingResultDto(false, null, "Cannot book a slot in the past.");
        }

        Appointment appointment = new()
        {
            DoctorId = schedule.DoctorId,
            PatientId = patientId,
            DoctorScheduleId = doctorScheduleId,
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(cancellationToken);

        return new BookingResultDto(true, appointment.Id, null);
    }

    public async Task CancelAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        Appointment appointment = await GetTrackedAppointmentAsync(appointmentId, cancellationToken);

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel an appointment with status {appointment.Status}.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        Appointment appointment = await GetTrackedAppointmentAsync(appointmentId, cancellationToken);

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot complete a cancelled appointment.");
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            return;
        }

        appointment.Status = AppointmentStatus.Completed;
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<AppointmentDto?> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken = default) =>
        ProjectAppointments(db.Appointments.AsNoTracking().Where(a => a.Id == appointmentId))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AppointmentDto>> GetPatientAppointmentsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default) =>
        await ProjectAppointments(
                db.Appointments.AsNoTracking()
                    .Where(a => a.PatientId == patientId)
                    .OrderByDescending(a => a.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AppointmentDto>> GetDoctorAppointmentsAsync(
        Guid doctorId,
        CancellationToken cancellationToken = default) =>
        await ProjectAppointments(
                db.Appointments.AsNoTracking()
                    .Where(a => a.DoctorId == doctorId)
                    .OrderByDescending(a => a.CreatedAt))
            .ToListAsync(cancellationToken);

    private async Task<Appointment> GetTrackedAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        Appointment? appointment = await db.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            throw new InvalidOperationException("Appointment not found.");
        }

        return appointment;
    }

    private static IQueryable<AppointmentDto> ProjectAppointments(IQueryable<Appointment> query) =>
        query.Select(a => new AppointmentDto(
                a.Id,
                a.DoctorId,
                a.Doctor!.Name ?? string.Empty,
                a.PatientId,
                a.Patient!.FullName ?? string.Empty,
                a.DoctorScheduleId,
                a.DoctorSchedule!.StartTime,
                a.DoctorSchedule.EndTime,
                a.Status == AppointmentStatus.Pending ? "Pending" :
                a.Status == AppointmentStatus.Confirmed ? "Confirmed" :
                a.Status == AppointmentStatus.Completed ? "Completed" : "Cancelled",
                a.CreatedAt));
}
