using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
    public DbSet<TeamsMessage> TeamsMessages { get; set; }
    public DbSet<WorkLog> WorkLogs { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<TaskComment> TaskComments { get; set; }
    public DbSet<PendingConversation> PendingConversations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.AzureId)
            .IsUnique();

        modelBuilder.Entity<TeamsMessage>()
            .HasIndex(x => x.GraphMessageId)
            .IsUnique();

        modelBuilder.Entity<TaskItem>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaskComment>()
            .HasOne<TaskItem>()
            .WithMany()
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskComment>()
            .HasOne<User>()
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