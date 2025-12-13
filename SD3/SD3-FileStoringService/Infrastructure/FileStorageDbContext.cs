using Microsoft.EntityFrameworkCore;
using SD3_FileStoringService.Models;
using SD3_FileStoringService.Models.Values;

namespace SD3_FileStoringService.Infrastructure;

public class FileStorageDbContext : DbContext
{
  public DbSet<FileMetadata> Files { get; set; } = null!;

  public FileStorageDbContext(DbContextOptions<FileStorageDbContext> options)
      : base(options)
  {
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<FileMetadata>(entity =>
    {
      entity.HasKey(f => f.FileId);
      entity.Property(f => f.FileId).ValueGeneratedNever();

      entity.Property(f => f.StudentId)
            .HasConversion(
                v => v.ToString(),
                v => new StudentId(v))
            .IsRequired()
            .HasMaxLength(100);

      entity.Property(f => f.AssignmentId)
            .HasConversion(
                v => v.ToString(),
                v => new AssignmentId(v))
            .IsRequired()
            .HasMaxLength(100);

      entity.Property(f => f.StoredPath)
            .HasConversion(
                v => v.ToString(),
                v => new FilePath(v))
            .IsRequired();

      entity.Property(f => f.UploadName)
            .HasConversion(
                v => v.ToString(),
                v => new FileName(v))
            .IsRequired();
      entity.Property(f => f.ContentType)
            .HasConversion(
                v => v.ToString(),
                v => new ContentType(v))
            .IsRequired();

      entity.Property(f => f.UploadTime)
            .IsRequired();

      entity.ToTable("files");
    });
  }
}