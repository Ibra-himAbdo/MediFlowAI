namespace MediFlowAI.Infrastructure;

public class SpecializationConfiguration : IEntityTypeConfiguration<Specialization>
{
    public void Configure(EntityTypeBuilder<Specialization> builder)
    {
        builder.ToTable("Specializations");
        
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(s => s.Name)
            .IsUnique();

        builder.HasMany(s => s.Doctors)
            .WithOne(d => d.Specialization)
            .HasForeignKey(d => d.SpecializationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
