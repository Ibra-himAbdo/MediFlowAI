namespace MediFlowAI.Infrastructure;

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("DoctorSchedules", t =>
            t.HasCheckConstraint("CK_DoctorSchedule_TimeRange", "[EndTime] > [StartTime]"));
        
        builder.HasIndex(s => s.DoctorId);

        builder.HasIndex(s => new { s.DoctorId, s.StartTime });

        builder.HasOne(s => s.Appointment)
            .WithOne(a => a.DoctorSchedule)
            .HasForeignKey<Appointment>(a => a.DoctorScheduleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
