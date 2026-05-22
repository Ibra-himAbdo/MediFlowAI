namespace MediFlowAI.Infrastructure;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");
        
        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.ConsultationFee)
            .HasPrecision(18, 2);

        builder.Property(d => d.AverageRate)
            .HasDefaultValue(0d);

        builder.Property(d => d.TotalReviews)
            .HasDefaultValue(0);

        builder.Property(d => d.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(d => d.SpecializationId);

        builder.HasMany(d => d.Schedules)
            .WithOne(s => s.Doctor)
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Appointments)
            .WithOne(a => a.Doctor)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Reviews)
            .WithOne(r => r.Doctor)
            .HasForeignKey(r => r.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
