using SD3_FileAnalysisService.Models.Values;

namespace SD3_FileAnalysisService.Models;

public class FileMetadata
{
  public Guid FileId { get; init; }
  public StudentId StudentId { get; init; }
  public AssignmentId AssignmentId { get; init; }
  public FilePath StoredPath { get; init; }
  public FileName UploadName { get; init; }
  public ContentType ContentType { get; init; }
  public DateTime UploadTime { get; init; }

  public FileMetadata(Guid fileId, StudentId studentId, AssignmentId assignmentId, FilePath storedPath, FileName uploadName, ContentType contentType, DateTime uploadTime)
  {
    if (fileId == Guid.Empty)
    {
      throw new ArgumentException("File ID cannot be empty.", nameof(fileId));
    }

    FileId = fileId;
    StudentId = studentId;
    AssignmentId = assignmentId;
    StoredPath = storedPath;
    UploadName = uploadName;
    ContentType = contentType;
    UploadTime = uploadTime;
  }

  public static FileMetadata Create(StudentId studentId, AssignmentId assignmentId, FilePath storedPath, FileName uploadName, ContentType contentType)
  {
    return new FileMetadata(Guid.NewGuid(), studentId, assignmentId, storedPath, uploadName, contentType, DateTime.UtcNow);
  }
}