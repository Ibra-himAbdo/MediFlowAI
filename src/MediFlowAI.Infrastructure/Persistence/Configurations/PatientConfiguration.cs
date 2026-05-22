namespace MediFlowAI.Infrastructure;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        
        builder.Property(p => p.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(p => p.PhoneNumber);

        builder.HasMany(p => p.Appointments)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Reviews)
            .WithOne(r => r.Patient)
            .HasForeignKey(r => r.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
