using BookonnectAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookonnectAPI.Data;

// Adds default values for the InventoryLog. Using IEntityTypeConfiguration to create/update migration file
public class InventoryLogConfiguration : IEntityTypeConfiguration<InventoryLog>
{
    public void Configure(EntityTypeBuilder<InventoryLog> builder)
    {
        // SQLite uses CURRENT_TIMESTAMP while MySQL server uses NOW()
        builder.Property(e => e.DateTime)
            .HasDefaultValueSql("NOW()");
        builder.Property(e => e.Type)
            .HasDefaultValue(ChangeType.InitialStock);
    }
}

