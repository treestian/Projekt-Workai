using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<SyncSettings> SyncSettings => Set<SyncSettings>();
    public DbSet<WorkLog> WorkLogs { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<TaskComment> TaskComments { get; set; }
    public DbSet<PendingConversation> PendingConversations { get; set; }

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();

        configurationBuilder
            .Properties<DateTime?>()
            .HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.AzureId)
            .IsUnique();

        modelBuilder.Entity<TaskItem>()
            .HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TaskComment>()
            .HasOne(x => x.Task)
            .WithMany(task => task.Comments)
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TaskComment>()
            .HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);



        modelBuilder.Entity<PendingConversation>()
            .Property(x => x.Messages)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(
                    v,
                    (System.Text.Json.JsonSerializerOptions?)null),

                v => System.Text.Json.JsonSerializer.Deserialize<
                    List<ConversationMessage>>(
                    v,
                    (System.Text.Json.JsonSerializerOptions?)null
                ) ?? new List<ConversationMessage>())
            .Metadata.SetValueComparer(
                new ValueComparer<List<ConversationMessage>>(
                    (a, b) =>
                        a != null &&
                        b != null &&
                        a.SequenceEqual(b),

                    c =>
                        c.Aggregate(
                            0,
                            (hash, message) =>
                                HashCode.Combine(
                                    hash,
                                    message.Role,
                                    message.Content)),

                    c =>
                        c.Select(message =>
                            new ConversationMessage
                            {
                                Role = message.Role,
                                Content = message.Content
                            }).ToList()));
    }
}

public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

public class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            v => v,
            v => v.HasValue
                ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
                : v)
    {
    }
}