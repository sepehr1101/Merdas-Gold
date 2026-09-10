using MerdasGold.Features.Settings.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MerdasGold.Features.Settings.Data;

public sealed class SecuritySettingsConfiguration : IEntityTypeConfiguration<SecuritySettings>
{
    public void Configure(EntityTypeBuilder<SecuritySettings> builder)
    {
        builder.ToTable("SecuritySettings", table =>
        {
            table.HasCheckConstraint(
                "CK_SecuritySettings_MinimumPasswordLength",
                "[MinimumPasswordLength] BETWEEN 6 AND 128");
            table.HasCheckConstraint(
                "CK_SecuritySettings_MaxFailedLoginAttempts",
                "[MaxFailedLoginAttempts] BETWEEN 1 AND 20");
            table.HasCheckConstraint(
                "CK_SecuritySettings_LockoutDurationMinutes",
                "[LockoutDurationMinutes] BETWEEN 1 AND 1440");
            table.HasCheckConstraint(
                "CK_SecuritySettings_SessionTimeoutMinutes",
                "[SessionTimeoutMinutes] BETWEEN 5 AND 10080");
            table.HasCheckConstraint(
                "CK_SecuritySettings_SingleRow",
                "[Id] = 1");
        });

        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Id).ValueGeneratedNever();
        builder.Property(settings => settings.RowVersion).IsRowVersion();

        builder.HasData(new
        {
            Id = SecuritySettings.SingletonId,
            MinimumPasswordLength = 12,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireSpecialCharacter = true,
            MaxFailedLoginAttempts = 5,
            LockoutDurationMinutes = 15,
            SessionTimeoutMinutes = 60
        });
    }
}
