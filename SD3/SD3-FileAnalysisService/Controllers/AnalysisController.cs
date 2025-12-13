using Microsoft.AspNetCore.Mvc;
using SD3_FileAnalysisService.Infrastructure;
using SD3_FileAnalysisService.Models;
using SD3_FileAnalysisService.Models.Values;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SD3.FileAnalisysService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
  private readonly FileAnalysisDbContext _context;
  private readonly IWebHostEnvironment _environment;
  private readonly HttpClient _gatewayClient;
  private readonly HttpClient _wordCloudClient;

  public AnalysisController(
      FileAnalysisDbContext context,
      IWebHostEnvironment environment,
      IHttpClientFactory httpClientFactory)
  {
    _context = context;
    _environment = environment;
    _gatewayClient = httpClientFactory.CreateClient("GatewayClient");
    _wordCloudClient = httpClientFactory.CreateClient("WordCloudClient");
  }


  /// <summary>
  /// метод для анализа файла файла
  /// </summary>
  /// <param name="fileId"></param>
  /// <returns>отчет по плагиату</returns>
  [HttpPost("analyze-file")]
  public IActionResult AnalyzeFile([FromForm] Guid fileId)
  {

    //в этом блоке запрашиваем метаданные файла
    var metadataRequest = new HttpRequestMessage(
        HttpMethod.Get,
        $"/api/gateway/files/metadata/{fileId}");

    var metadataResponse = _gatewayClient.Send(metadataRequest);

    if (!metadataResponse.IsSuccessStatusCode)
    {
      var error = metadataResponse.Content.ReadAsStringAsync().Result;
      return StatusCode(500, new { error = "Failed to fetch file metadata from gateway" });
    }

    var metadataJson = metadataResponse.Content.ReadAsStringAsync().Result;
    using var metaDoc = JsonDocument.Parse(metadataJson);
    var uploadNameElement = metaDoc.RootElement.GetProperty("uploadName");
    var uploadName = uploadNameElement.GetString();

    var assignment = metaDoc.RootElement.GetProperty("assignmentId").GetString();
    var student = metaDoc.RootElement.GetProperty("studentId").GetString();
    var upload = DateTime.Parse(metaDoc.RootElement.GetProperty("uploadTime").GetString());

    if (string.IsNullOrEmpty(uploadName))
    {
      return StatusCode(500, new { error = "File metadata does not contain uploadName" });
    }

    var fileExtension = Path.GetExtension(uploadName).ToLowerInvariant();
    bool isTextFile = fileExtension == ".txt";



    string? fileText = null;


    //если файл txt - То можно составить облако слов, делаем это
    if (isTextFile)
    {
      var fileRequest = new HttpRequestMessage(
          HttpMethod.Get,
          $"/api/gateway/files/{fileId}");

      var fileResponse = _gatewayClient.Send(fileRequest);

      if (!fileResponse.IsSuccessStatusCode)
      {
        var error = fileResponse.Content.ReadAsStringAsync().Result;
        return StatusCode(500, new { error = "Failed to download file content" });
      }

      var fileBytes = fileResponse.Content.ReadAsByteArrayAsync().Result;
      try
      {
        fileText = Encoding.UTF8.GetString(fileBytes);
      }
      catch (DecoderFallbackException)
      {
        fileText = Encoding.GetEncoding("windows-1251").GetString(fileBytes);
      }
    }

    byte[] wordCloudImageBytes = null;

    if (fileText != null && fileText.Count() > 0)
    {
      var requestData = new
      {
        format = "png",
        width = 800,
        height = 600,
        fontFamily = "Arial",
        scale = "sqrt",
        text = fileText
      };


      var jsonToSend = JsonSerializer.Serialize(requestData);
      var content = new StringContent(jsonToSend, Encoding.UTF8, "application/json");


      var quickChartResponse = _wordCloudClient.PostAsync("", content).Result;


      if (quickChartResponse.IsSuccessStatusCode)
      {
        wordCloudImageBytes = quickChartResponse.Content.ReadAsByteArrayAsync().Result;
      }
      else
      {
        var error = quickChartResponse.Content.ReadAsStringAsync().Result;
      }
    }

    WordCloudPath? wordCloudPath = null;

    if (wordCloudImageBytes != null && wordCloudImageBytes.Length > 0)
    {
      try
      {
        var wordCloudsDir = Path.Combine(_environment.ContentRootPath, "wordclouds");
        Directory.CreateDirectory(wordCloudsDir);

        var fileName = $"{Guid.NewGuid()}.png";
        var fullPath = Path.Combine(wordCloudsDir, fileName);

        using (var fs = new FileStream(fullPath, FileMode.Create))
        {
          fs.Write(wordCloudImageBytes, 0, wordCloudImageBytes.Length);
        }

        wordCloudPath = new WordCloudPath(fullPath);
      }
      catch (Exception ex)
      {
      }
    }

    //запрашиваем все работы по номеру задания
    var request = new HttpRequestMessage(
        HttpMethod.Get,
        $"/api/gateway/files/by-assignment/{assignment}");

    var response = _gatewayClient.Send(request);

    if (!response.IsSuccessStatusCode)
    {
      var error = response.Content.ReadAsStringAsync().Result;
      return StatusCode((int)response.StatusCode, new { error = "Failed to fetch submissions from gateway" });
    }

    var json = response.Content.ReadAsStringAsync().Result;



    var submissions = JsonSerializer.Deserialize<List<SubmissionDto>>(json);

    var currentFileRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/gateway/files/{fileId}");
    var currentFileResponse = _gatewayClient.Send(currentFileRequest);
    if (!currentFileResponse.IsSuccessStatusCode)
    {
      return StatusCode(500, new { error = "Failed to download current file for comparison" });
    }
    var currentFileBytes = currentFileResponse.Content.ReadAsByteArrayAsync().Result;

    bool isPlagiarism = false;
    Guid? plagiarizedFileId = null;

    //смотрим на все файлы по заданию
    foreach (var submission in submissions)
    {

      if (submission.StudentId == student || DateTime.Parse(submission.UploadTime) > upload)
      {
        continue;
      }
      //запрашиваем файл и побайтово проверяем
      var candidateRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/gateway/files/{submission.FileId}");
      var candidateResponse = _gatewayClient.Send(candidateRequest);

      if (!candidateResponse.IsSuccessStatusCode)
        continue;

      var candidateBytes = candidateResponse.Content.ReadAsByteArrayAsync().Result;

      if (currentFileBytes.SequenceEqual(candidateBytes))
      {
        isPlagiarism = true;
        plagiarizedFileId = submission.FileId;
        break;
      }

    }

    //составляем отчет и отправляем
    string reportText = isPlagiarism
        ? $"Plagiarism detected: identical file found (fileId: {plagiarizedFileId})"
        : "No plagiarism detected. Original work.";

    int percentage = isPlagiarism ? 100 : 0;

    var reportEntity = AnalysisReport.Create(
      fileId,
      student!,
      assignment!,
      isPlagiarism,
      percentage,
      reportText,
      wordCloudPath);

    try
    {
      _context.Reports.Add(reportEntity);
      _context.SaveChanges();
    }
    catch (Exception ex)
    {
      return StatusCode(500, new { error = $"Failed to save report: {ex.Message}" });
    }

    return Ok(new
    {
      reportId = reportEntity.ReportId,
      isPlagiarismDetected = reportEntity.IsPlagiarismDetected,
      plagiarismPercentage = reportEntity.PlagiarismPercentage,
      analysisTime = reportEntity.AnalysisTime,
      hasWordCloud = reportEntity.WordCloudPath != null,
      wordCloudUrl = reportEntity.WordCloudPath != null
        ? $"/api/analysis/{reportEntity.ReportId}/wordcloud"
        : (string?)null
    });
  }


  /// <summary>
  /// получает отчет по его айди
  /// </summary>
  /// <param name="reportId"></param>
  /// <returns></returns>
  [HttpGet("{reportId}")]
  public IActionResult GetReport(Guid reportId)
  {
    var report = _context.Reports.Find(reportId);
    if (report == null)
    {
      return NotFound(new { error = "Report not found" });
    }

    return Ok(new
    {
      reportId = report.ReportId,
      fileId = report.FileId,
      studentId = report.StudentId.ToString(),
      assignmentId = report.AssignmentId.ToString(),
      isPlagiarismDetected = report.IsPlagiarismDetected,
      plagiarismPercentage = report.PlagiarismPercentage,
      reportContent = report.ReportContent,
      analysisTime = report.AnalysisTime
    });
  }



  /// <summary>
  /// получает список всех отчетов по номеру задания
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
    var reports = _context.Reports
        .Where(f => f.AssignmentId == checkedAssigmentId)
        .Select(f => new
        {
          studentId = f.StudentId.ToString(),
          assignmentId = f.AssignmentId.ToString(),
          reportId = f.ReportId,
          fileId = f.FileId,
          isPlagiarismDetected = f.IsPlagiarismDetected,
          plagiarismPercentage = f.PlagiarismPercentage,
          reportContent = f.ReportContent,
          analysisTime = f.AnalysisTime
        })
        .ToList();

    return Ok(reports);
  }


  /// <summary>
  /// дает файл облака слов по айди репорта
  /// </summary>
  /// <param name="reportId"></param>
  /// <returns></returns>
  [HttpGet("{reportId}/wordcloud")]
  public IActionResult GetWordCloud(Guid reportId)
  {
    var report = _context.Reports.Find(reportId);
    if (report == null)
    {
      return NotFound(new { error = "Report not found" });
    }

    if (report.WordCloudPath == null)
    {
      return NotFound(new { error = "Word cloud not available for this report" });
    }

    var filePath = report.WordCloudPath.Value;

    if (!System.IO.File.Exists(filePath))
    {
      return StatusCode(500, new { error = "Word cloud file missing on disk" });
    }

    byte[] fileBytes;
    try
    {
      fileBytes = System.IO.File.ReadAllBytes(filePath);
    }
    catch (Exception ex)
    {
      return StatusCode(500, new { error = $"Failed to read word cloud file: {ex.Message}" });
    }

    return File(fileBytes, "image/png", $"{reportId}.png");
  }

  //dtoшка для файловой метадаты
  private class SubmissionDto
  {
    [JsonPropertyName("fileId")]
    public Guid FileId { get; set; }

    [JsonPropertyName("assignmentId")]
    public string AssignmentId { get; set; } = null!;

    [JsonPropertyName("studentId")]
    public string StudentId { get; set; } = null!;

    [JsonPropertyName("uploadName")]
    public string UploadName { get; set; } = null!;

    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = null!;

    [JsonPropertyName("uploadTime")]
    public string UploadTime { get; set; }
  }
}