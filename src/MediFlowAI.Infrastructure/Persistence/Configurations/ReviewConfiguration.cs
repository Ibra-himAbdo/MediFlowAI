namespace MediFlowAI.Infrastructure;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", t =>
            t.HasCheckConstraint("CK_Review_Rate", "[Rate] >= 1 AND [Rate] <= 5"));
        
        builder.Property(r => r.Comment)
            .HasMaxLength(1000);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.HasIndex(r => r.AppointmentId)
            .IsUnique();

        builder.HasIndex(r => r.DoctorId);
        builder.HasIndex(r => r.PatientId);
    }
}
