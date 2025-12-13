using Microsoft.AspNetCore.Mvc;
using SD3_FileStoringService.Infrastructure;
using SD3_FileStoringService.Models;
using SD3_FileStoringService.Models.Values;


namespace SD3.FileStoringService.Controllers;


[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
  private readonly FileStorageDbContext _context;
  private readonly IWebHostEnvironment _environment;


  public FilesController(FileStorageDbContext context, IWebHostEnvironment environment)
  {
    _context = context;
    _environment = environment;
  }


  /// <summary>
  /// POST-метода, получает id студента и задания, файл, сохраняет файл на диск, метадату в бд
  /// </summary>
  /// <param name="studentId"></param>
  /// <param name="assignmentId"></param>
  /// <param name="file"></param>
  /// <returns>возвращает некоторую метадату файла</returns>
  [HttpPost]
  public IActionResult UploadFile([FromForm] string studentId, [FromForm] string assignmentId, IFormFile file)
  {
    StudentId student;
    AssignmentId assignment;
    try
    {
      student = new StudentId(studentId);
      assignment = new AssignmentId(assignmentId);
    }
    catch (ArgumentException ex)
    {
      return BadRequest(new { error = ex.Message });
    }

    if (file == null || file.Length == 0)
    {
      return BadRequest("File is required and must not be empty.");
    }


    var uploadDir = Path.Combine(_environment.ContentRootPath, "uploads");
    Directory.CreateDirectory(uploadDir);

    var safeFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
    var fullPath = Path.Combine(uploadDir, safeFileName);

    try
    {
      using (var stream = new FileStream(fullPath, FileMode.Create))
      {
        file.CopyTo(stream);
      }
    }
    catch (Exception ex)
    {
      return StatusCode(500, new { error = $"Failed to save file: {ex.Message}" });
    }

    FileName uploadName = file.FileName;
    FilePath storedPath = new FilePath(fullPath);
    ContentType type = new ContentType(file.ContentType);

    var metadata = FileMetadata.Create(student, assignment, storedPath, uploadName, type);

    try
    {
      _context.Files.Add(metadata);
      _context.SaveChanges();
    }
    catch (Exception ex)
    {
      if (System.IO.File.Exists(fullPath))
      {
        System.IO.File.Delete(fullPath);
      }

      return StatusCode(500, new { error = $"Failed to save metadata to database: {ex.Message}" });
    }

    return Ok(new
    {
      fileId = metadata.FileId,
      uploadTime = metadata.UploadTime,
      downloadUrl = $"/api/files/{metadata.FileId}",
      type = file.ContentType
    });
  }


  /// <summary>
  /// возвращает файл по его ID
  /// </summary>
  /// <param name="fileId"></param>
  /// <returns></returns>
  [HttpGet("{fileId}")]
  public IActionResult DownloadFile(Guid fileId)
  {
    var metadata = _context.Files.Find(fileId);
    if (metadata == null)
    {
      return NotFound(new { error = "File not found" });
    }

    var filePath = metadata.StoredPath.ToString();
    if (!System.IO.File.Exists(filePath))
    {
      return StatusCode(500, new { error = "File exists in database but missing on disk" });
    }

    byte[] fileBytes;
    try
    {
      fileBytes = System.IO.File.ReadAllBytes(filePath);
    }
    catch (Exception ex)
    {
      return StatusCode(500, new { error = $"Failed to read file: {ex.Message}" });
    }

    var contentType = metadata.ContentType;
    var fileName = metadata.UploadName;

    return File(fileBytes, contentType, fileName);
  }


  /// <summary>
  /// удаляет файл по его ID
  /// </summary>
  /// <param name="fileId"></param>
  /// <returns></returns>
  [HttpDelete("{fileId}")]
  public IActionResult DeleteFile(Guid fileId)
  {
    var metadata = _context.Files.Find(fileId);
    if (metadata == null)
    {
      return NotFound(new { error = "File not found" });
    }

    var filePath = metadata.StoredPath.Value;
    if (System.IO.File.Exists(filePath))
    {
      try
      {
        System.IO.File.Delete(filePath);
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { error = $"Failed to delete file from disk: {ex.Message}" });
      }
    }

    try
    {
      _context.Files.Remove(metadata);
      _context.SaveChanges();
    }
    catch (Exception ex)
    {
      return StatusCode(500, new { error = $"Failed to delete metadata from database: {ex.Message}" });
    }

    return Ok(new { message = "File deleted successfully" });
  }


  /// <summary>
  /// получает список всех сданных работ по номеру задания
  /// </summary>
  /// <param name="assignmentId"></param>
  /// <returns></returns>
  [HttpGet("by-assignment/{assignmentId}")]
  public IActionResult GetFilesByAssignment(string assignmentId)
  {
    if (string.IsNullOrWhiteSpace(assignmentId))
    {
      return BadRequest("AssignmentId is required.");
    }

    try
    {
      var assignment = new AssignmentId(assignmentId);
    }
    catch (ArgumentException ex)
    {
      return BadRequest(new { error = ex.Message });
    }

    var checkedAssigmentId = new AssignmentId(assignmentId);
    var files = _context.Files
        .Where(f => f.AssignmentId == checkedAssigmentId)
        .Select(f => new
        {
          fileId = f.FileId,
          studentId = f.StudentId.ToString(),
          assignmentId = f.AssignmentId.ToString(),
          uploadName = f.UploadName.ToString(),
          contentType = f.ContentType.ToString(),
          uploadTime = f.UploadTime
        })
        .ToList();

    return Ok(files);
  }


  /// <summary>
  /// получает метадату файла по его id
  /// </summary>
  /// <param name="fileId"></param>
  /// <returns></returns>
  [HttpGet("metadata/{fileId}")]
  public IActionResult GetFileMetadata(Guid fileId)
  {
    var metadata = _context.Files.Find(fileId);
    if (metadata == null)
    {
      return NotFound(new { error = "File not found" });
    }

    return Ok(new
    {
      fileId = metadata.FileId,
      studentId = metadata.StudentId.ToString(),
      assignmentId = metadata.AssignmentId.ToString(),
      uploadName = metadata.UploadName.ToString(),
      contentType = metadata.ContentType.ToString(),
      uploadTime = metadata.UploadTime.ToString()
    });
  }
}