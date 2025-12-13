using SD3_FileAnalysisService.Models.Values;

namespace SD3_FileAnalysisService.Models;

public class AnalysisReport
{
  public Guid ReportId { get; private set; }
  public Guid FileId { get; private set; }
  public StudentId StudentId { get; private set; }
  public AssignmentId AssignmentId { get; private set; }
  public bool IsPlagiarismDetected { get; private set; }
  public int PlagiarismPercentage { get; private set; }
  public string ReportContent { get; private set; } = string.Empty;
  public DateTime AnalysisTime { get; private set; }
  public WordCloudPath? WordCloudPath { get; private set; }


  private AnalysisReport() { }

  public static AnalysisReport Create(
      Guid fileId,
      StudentId studentId,
      AssignmentId assignmentId,
      bool isPlagiarismDetected,
      int plagiarismPercentage,
      string reportContent,
      WordCloudPath? wordCloudPath = null)
  {
    return new AnalysisReport
    {
      ReportId = Guid.NewGuid(),
      FileId = fileId,
      StudentId = studentId,
      AssignmentId = assignmentId,
      IsPlagiarismDetected = isPlagiarismDetected,
      PlagiarismPercentage = plagiarismPercentage,
      ReportContent = reportContent,
      AnalysisTime = DateTime.UtcNow,
      WordCloudPath = wordCloudPath,
    };
  }
}