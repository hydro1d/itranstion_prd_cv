using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CvPlatform.Data.Entities;

namespace CvPlatform.Data;

/// <summary>
/// Core EF Core DbContext for the CV Management Platform.
/// Configures all domain entities, relationships, indexes, and unique constraints.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Dynamic Reusable Attribute Library (Killer Feature #1)
    public DbSet<CvAttribute> Attributes => Set<CvAttribute>();
    public DbSet<AttributeCategory> AttributeCategories => Set<AttributeCategory>();
    public DbSet<AttributeOption> AttributeOptions => Set<AttributeOption>();

    // Positions & Opportunity Templates (Killer Feature #2)
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PositionAttribute> PositionAttributes => Set<PositionAttribute>();

    // Candidate Profile & Portfolio Projects
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<ProfileAttributeValue> ProfileAttributeValues => Set<ProfileAttributeValue>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();
    public DbSet<ProjectTagLink> ProjectTagLinks => Set<ProjectTagLink>();

    // Tailored CV Engine (Killer Feature #3)
    public DbSet<CV> CVs => Set<CV>();
    public DbSet<CVAttributeValue> CVAttributeValues => Set<CVAttributeValue>();
    public DbSet<CVLike> CVLikes => Set<CVLike>();

    // Collaboration & Discussions
    public DbSet<Discussion> Discussions => Set<Discussion>();
    public DbSet<DiscussionMessage> DiscussionMessages => Set<DiscussionMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. PositionAttribute (Composite Key)
        builder.Entity<PositionAttribute>(entity =>
        {
            entity.HasKey(pa => new { pa.PositionId, pa.AttributeId });

            entity.HasOne(pa => pa.Position)
                  .WithMany(p => p.PositionAttributes)
                  .HasForeignKey(pa => pa.PositionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pa => pa.Attribute)
                  .WithMany(a => a.PositionAttributes)
                  .HasForeignKey(pa => pa.AttributeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 2. ProjectTagLink (Composite Key)
        builder.Entity<ProjectTagLink>(entity =>
        {
            entity.HasKey(pt => new { pt.ProjectId, pt.TagId });

            entity.HasOne(pt => pt.Project)
                  .WithMany(p => p.TagLinks)
                  .HasForeignKey(pt => pt.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pt => pt.Tag)
                  .WithMany(t => t.ProjectLinks)
                  .HasForeignKey(pt => pt.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 3. CandidateProfile (1-to-1 with ApplicationUser)
        builder.Entity<CandidateProfile>(entity =>
        {
            entity.HasIndex(cp => cp.UserId).IsUnique();

            entity.HasOne(cp => cp.User)
                  .WithOne(u => u.CandidateProfile)
                  .HasForeignKey<CandidateProfile>(cp => cp.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 4. CV: STRICT RULE — One CV per candidate per position
        builder.Entity<CV>(entity =>
        {
            entity.HasIndex(cv => new { cv.CandidateProfileId, cv.PositionId })
                  .IsUnique();

            entity.HasOne(cv => cv.CandidateProfile)
                  .WithMany(cp => cp.CVs)
                  .HasForeignKey(cv => cv.CandidateProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cv => cv.Position)
                  .WithMany(p => p.CVs)
                  .HasForeignKey(cv => cv.PositionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(cv => cv.CreatedAt);
        });

        // 5. CVLike: STRICT RULE — One like per recruiter per CV
        builder.Entity<CVLike>(entity =>
        {
            entity.HasIndex(like => new { like.RecruiterId, like.CVId })
                  .IsUnique();

            entity.HasOne(like => like.CV)
                  .WithMany(cv => cv.Likes)
                  .HasForeignKey(like => like.CVId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(like => like.Recruiter)
                  .WithMany(u => u.GivenLikes)
                  .HasForeignKey(like => like.RecruiterId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 6. ProfileAttributeValue (Unique attribute per candidate profile)
        builder.Entity<ProfileAttributeValue>(entity =>
        {
            entity.HasIndex(pav => new { pav.CandidateProfileId, pav.AttributeId })
                  .IsUnique();

            entity.HasOne(pav => pav.CandidateProfile)
                  .WithMany(cp => cp.AttributeValues)
                  .HasForeignKey(pav => pav.CandidateProfileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pav => pav.Attribute)
                  .WithMany(a => a.ProfileValues)
                  .HasForeignKey(pav => pav.AttributeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 7. CVAttributeValue (Unique attribute per CV)
        builder.Entity<CVAttributeValue>(entity =>
        {
            entity.HasIndex(cav => new { cav.CVId, cav.AttributeId })
                  .IsUnique();

            entity.HasOne(cav => cav.CV)
                  .WithMany(cv => cv.AttributeValues)
                  .HasForeignKey(cav => cav.CVId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cav => cav.Attribute)
                  .WithMany(a => a.CVValues)
                  .HasForeignKey(cav => cav.AttributeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 8. Position
        builder.Entity<Position>(entity =>
        {
            entity.HasOne(p => p.Recruiter)
                  .WithMany(u => u.CreatedPositions)
                  .HasForeignKey(p => p.RecruiterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => p.Title);
            entity.HasIndex(p => p.Company);
            entity.HasIndex(p => p.EmploymentType);
            entity.HasIndex(p => p.Visibility);
            entity.HasIndex(p => p.CreatedAt);
        });

        // 9. CvAttribute
        builder.Entity<CvAttribute>(entity =>
        {
            entity.HasIndex(a => a.Name);
            entity.HasIndex(a => a.CategoryId);
            entity.HasIndex(a => a.IsActive);

            entity.HasOne(a => a.Category)
                  .WithMany(c => c.Attributes)
                  .HasForeignKey(a => a.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 10. Discussion & Messages
        builder.Entity<Discussion>(entity =>
        {
            entity.HasOne(d => d.Position)
                  .WithMany(p => p.Discussions)
                  .HasForeignKey(d => d.PositionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DiscussionMessage>(entity =>
        {
            entity.HasOne(m => m.Discussion)
                  .WithMany(d => d.Messages)
                  .HasForeignKey(m => m.DiscussionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.User)
                  .WithMany(u => u.DiscussionMessages)
                  .HasForeignKey(m => m.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => m.SentAt);
        });
    }
}
