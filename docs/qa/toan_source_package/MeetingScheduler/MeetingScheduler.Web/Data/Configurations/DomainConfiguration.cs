using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MeetingScheduler.Web.Models;
namespace MeetingScheduler.Web.Data.Configurations;
public class DomainConfiguration : IEntityTypeConfiguration<User>, IEntityTypeConfiguration<Role>, IEntityTypeConfiguration<Department>, IEntityTypeConfiguration<Room>, IEntityTypeConfiguration<Meeting>, IEntityTypeConfiguration<MeetingParticipant>, IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("User"); b.HasIndex(x => x.Email).IsUnique(); b.Property(x => x.IsDisabled).HasDefaultValue(false);
        b.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Department).WithMany(x => x.Users).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<Role> b)
    { b.ToTable("Role"); b.Property(x => x.RoleId).ValueGeneratedNever(); b.HasIndex(x => x.RoleName).IsUnique(); }
    public void Configure(EntityTypeBuilder<Department> b)
    { b.ToTable("Department"); b.HasIndex(x => x.DepartmentName).IsUnique(); b.Property(x => x.IsDisabled).HasDefaultValue(false); }
    public void Configure(EntityTypeBuilder<Room> b)
    {
        b.ToTable("Room", t => t.HasCheckConstraint("CK_Room_Capacity", "[Capacity] > 0"));
        b.HasIndex(x => x.RoomName).IsUnique(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.IsDisabled).HasDefaultValue(false);
    }
    public void Configure(EntityTypeBuilder<Meeting> b)
    {
        b.ToTable("Meeting", t => t.HasCheckConstraint("CK_Meeting_Time", "[EndTime] > [StartTime]"));
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => new { x.RoomId, x.StartTime, x.EndTime });
        b.HasOne(x => x.Room).WithMany(x => x.Meetings).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CreatedByUser).WithMany(x => x.OrganizedMeetings).HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CancelledByUser).WithMany(x => x.CancelledMeetings).HasForeignKey(x => x.CancelledByUserId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<MeetingParticipant> b)
    {
        b.ToTable("MeetingParticipant"); b.HasKey(x => new { x.MeetingId, x.UserId });
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Meeting).WithMany(x => x.Participants).HasForeignKey(x => x.MeetingId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.User).WithMany(x => x.MeetingParticipants).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLog");
        b.HasOne(x => x.User).WithMany(x => x.AuditLogs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
