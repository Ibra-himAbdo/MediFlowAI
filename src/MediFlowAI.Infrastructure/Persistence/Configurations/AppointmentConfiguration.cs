namespace MediFlowAI.Infrastructure;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        
        builder.Property(a => a.Status)
            .HasConversion<int>();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.HasIndex(a => a.DoctorScheduleId)
            .IsUnique();

        builder.HasIndex(a => a.DoctorId);
        builder.HasIndex(a => a.PatientId);
        builder.HasIndex(a => a.Status);

        builder.HasOne(a => a.Review)
            .WithOne(r => r.Appointment)
            .HasForeignKey<Review>(r => r.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
