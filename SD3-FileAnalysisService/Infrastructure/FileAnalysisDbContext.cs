using Microsoft.EntityFrameworkCore;
using SD3_FileAnalysisService.Models;
using SD3_FileAnalysisService.Models.Values;


namespace SD3_FileAnalysisService.Infrastructure;

public class FileAnalysisDbContext : DbContext
{
  public DbSet<AnalysisReport> Reports { get; set; } = null!;

  public FileAnalysisDbContext(DbContextOptions<FileAnalysisDbContext> options)
      : base(options) { }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<AnalysisReport>(entity =>
    {
      entity.HasKey(r => r.ReportId);
      entity.Property(r => r.ReportId).ValueGeneratedNever();

      entity.Property(r => r.FileId).IsRequired();
      entity.Property(r => r.ReportContent).IsRequired();

      entity.Property(r => r.StudentId)
            .HasConversion(v => v.Value, v => new StudentId(v))
            .IsRequired()
            .HasMaxLength(100);

      entity.Property(r => r.AssignmentId)
            .HasConversion(v => v.Value, v => new AssignmentId(v))
            .IsRequired()
            .HasMaxLength(100);

      entity.Property(r => r.AnalysisTime)
            .IsRequired();

      entity.Property(r => r.WordCloudPath)
            .HasConversion(v => v == null ? null : v.Value, v => v == null ? null : (WordCloudPath?)new WordCloudPath(v));

      entity.ToTable("reports");
    });
  }
}