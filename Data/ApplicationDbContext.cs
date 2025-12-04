using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkFlowBasic.Models;

namespace WorkFlowBasic.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Tablolarımız
    public DbSet<ProcessDefinition> ProcessDefinitions { get; set; }
    public DbSet<WorkflowRequest> WorkflowRequests { get; set; }
    public DbSet<RequestLog> RequestLogs { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }
    public DbSet<InventoryAssignment> InventoryAssignments { get; set; }
    public DbSet<BotKnowledge> BotKnowledges { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ApplicantHistory> ApplicantHistories { get; set; }
    public DbSet<WorkTask> WorkTasks { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ÖZEL AYARLAR (FLUENT API)

        // 1. Kural: Bir kullanıcının Yöneticisi (Manager) ile ilişkisi
        // Eğer bir Yönetici silinirse, ona bağlı personelin ManagerId'si NULL olsun (Kullanıcı silinmesin).
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Manager)
            .WithMany()
            .HasForeignKey(u => u.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // 2. Kural: Bir Talep silinirse, logları da silinsin (Cascade).
        builder.Entity<RequestLog>()
            .HasOne(l => l.WorkflowRequest)
            .WithMany(r => r.Logs)
            .HasForeignKey(l => l.WorkflowRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // 3. Kural: İşlem yapan kullanıcı (Processor) silinirse Log silinmesin, tarihçede kalsın.
        builder.Entity<RequestLog>()
            .HasOne(l => l.Processor)
            .WithMany()
            .HasForeignKey(l => l.ProcessorId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1. Kural: Görevi yapan personel silinirse, Görev SİLİNMESİN (Restrict)
        builder.Entity<WorkTask>()
            .HasOne(t => t.AssignedTo)
            .WithMany()
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict);

        // 2. Kural: Görevi atayan müdür silinirse, Görev SİLİNMESİN (Restrict)
        builder.Entity<WorkTask>()
            .HasOne(t => t.AssignedBy)
            .WithMany()
            .HasForeignKey(t => t.AssignedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}