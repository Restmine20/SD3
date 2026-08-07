using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace SD3.APIGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GatewayController : ControllerBase
{
  private readonly HttpClient _fileStoringClient;
  private readonly HttpClient _fileAnalysisClient;

  public GatewayController(IHttpClientFactory httpClientFactory)
  {
    _fileStoringClient = httpClientFactory.CreateClient("FileStoringService");
    _fileAnalysisClient = httpClientFactory.CreateClient("FileAnalysisService");
  }

  /// <summary>
  /// загружаем файл
  /// </summary>
  /// <param name="studentId">айди студента</param>
  /// <param name="assignmentId">айди задания</param>
  /// <param name="file">файл-решение</param>
  /// <returns>отчет о загруженном файле и его анализе</returns>
  [HttpPost("files")]
  public IActionResult UploadFile([FromForm] string studentId, [FromForm] string assignmentId, IFormFile file)
  {
    if (file == null || file.Length == 0)
      return BadRequest("File is required and must not be empty.");

    byte[] fileBytes;
    using (var ms = new MemoryStream())
    {
      file.CopyTo(ms);
      fileBytes = ms.ToArray();
    }

    using var content = new MultipartFormDataContent();

    var fileStream = new MemoryStream(fileBytes);
    var fileContent = new StreamContent(fileStream);
    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
    content.Add(fileContent, "file", file.FileName);

    content.Add(new StringContent(studentId), "studentId");
    content.Add(new StringContent(assignmentId), "assignmentId");

    var storingRequest = new HttpRequestMessage(HttpMethod.Post, "/api/files")
    {
      Content = content
    };

    var storingResponse = _fileStoringClient.Send(storingRequest);

    if (!storingResponse.IsSuccessStatusCode)
    {
      var errorContent = storingResponse.Content.ReadAsStringAsync().Result;
      return StatusCode((int)storingResponse.StatusCode, new { error = errorContent });
    }

    var storingResponseContent = storingResponse.Content.ReadAsStringAsync().Result;
    using var doc = JsonDocument.Parse(storingResponseContent);
    var root = doc.RootElement;
    if (!root.TryGetProperty("fileId", out var fileIdElement))
      return StatusCode(500, new { error = "FileStoringService did not return fileId" });

    var fileId = Guid.Parse(fileIdElement.GetString()!);

    using var analysisContent = new MultipartFormDataContent();
    analysisContent.Add(new StringContent(fileId.ToString()), "fileId");
    analysisContent.Add(new StringContent(studentId), "studentId");
    analysisContent.Add(new StringContent(assignmentId), "assignmentId");

    var analysisRequest = new HttpRequestMessage(HttpMethod.Post, "/api/analysis/analyze-file")
    {
      Content = analysisContent
    };

    var analysisResponse = _fileAnalysisClient.Send(analysisRequest);

    if (!analysisResponse.IsSuccessStatusCode)
    {
      var errorContent = analysisResponse.Content.ReadAsStringAsync().Result;
      return StatusCode((int)analysisResponse.StatusCode, new { error = errorContent });
    }

    var analysisResponseContent = analysisResponse.Content.ReadAsStringAsync().Result;
    return Ok(new
    {
      fileId = fileId,
      analysis = JsonDocument.Parse(analysisResponseContent).RootElement.Clone(),
      file.FileName
    });
  }

  
  /// <summary>
  /// загружаем файл по его айди
  /// </summary>
  /// <param name="fileId"></param>
  /// <returns></returns>
  [HttpGet("files/{fileId}")]
  public IActionResult DownloadFile(Guid fileId)
  {
    var request = new HttpRequestMessage(HttpMethod.Get, $"/api/files/{fileId}");

    var response = _fileStoringClient.Send(request);

    if (!response.IsSuccessStatusCode)
    {
      var errorContent = response.Content.ReadAsStringAsync().Result;
      return StatusCode((int)response.StatusCode, new { error = errorContent });
    }

    var fileBytes = response.Content.ReadAsByteArrayAsync().Result;
    var contentType = response.Content.Headers.ContentType?.ToString();
    var contentDisposition = response.Content.Headers.ContentDisposition;
    string fileName;
    if (!string.IsNullOrEmpty(contentDisposition?.FileNameStar))
    {
      var star = contentDisposition.FileNameStar;
      if (star.StartsWith("UTF-8''"))
      {
        var encoded = star["UTF-8''".Length..];
        fileName = Uri.UnescapeDataString(encoded.Replace('+', ' '));
      }
      else
      {
        fileName = contentDisposition.FileNameStar;
      }
    }
    else
    {
      fileName = contentDisposition?.FileName ?? "file";
    }

    return File(fileBytes, contentType, fileName);
  }

  
  /// <summary>
  /// получить отчет по его айди
  /// </summary>
  /// <param name="reportId"></param>
  /// <returns></returns>
  [HttpGet("reports/{reportId}")]
  public IActionResult GetReport(Guid reportId)
  {
    var request = new HttpRequestMessage(HttpMethod.Get, $"/api/analysis/{reportId}");

    var response = _fileAnalysisClient.Send(request);

    if (!response.IsSuccessStatusCode)
    {
      var errorContent = response.Content.ReadAsStringAsync().Result;
      return StatusCode((int)response.StatusCode, new { error = errorContent });
    }

    var reportContent = response.Content.ReadAsStringAsync().Result;
    var report = System.Text.Json.JsonSerializer.Deserialize<object>(reportContent);

    return Ok(report);
  }


  /// <summary>
  /// получает все файлы по заданию
  /// </summary>
  /// <param name="assignmentId"></param>
  /// <returns></returns>
  [HttpGet("files/by-assignment/{assignmentId}")]
  public IActionResult GetFilesByAssignment(string assignmentId)
  {
    if (string.IsNullOrWhiteSpace(assignmentId))
      return BadRequest("AssignmentId is required.");

    var request = new HttpRequestMessage(
        HttpMethod.Get,
        $"/api/files/by-assignment/{assignmentId}");

    var response = _fileStoringClient.Send(request);

    if (!response.IsSuccessStatusCode)
    {
      var errorContent = response.Content.ReadAsStringAsync().Result;
      return StatusCode((int)response.StatusCode, new { error = errorContent });
    }

    var json = response.Content.ReadAsStringAsync().Result;
    return Content(json, "application/json");
  }



  /// <summary>
  /// получает все файлы по заданию
  /// </summary>
  /// <param name="assignmentId"></param>
  /// <returns></returns>
  [HttpGet("reports/by-assignment/{assignmentId}")]
  public IActionResult GetReportsByAssignment(string assignmentId)
  {
    if (string.IsNullOrWhiteSpace(assignmentId))
      return BadRequest("AssignmentId is required.");

    var request = new HttpRequestMessage(
        HttpMethod.Get,
        $"/api/analysis/by-assignment/{assignmentId}");

    var response = _fileAnalysisClient.Send(request);

    if (!response.IsSuccessStatusCode)
    {
      var errorContent = response.Content.ReadAsStringAsync().Result;
      return StatusCode((int)response.StatusCode, new { error = errorContent });
    }

    var json = response.Content.ReadAsStringAsync().Result;
    return Content(json, "application/json");
  }


  /// <summary>
  /// получает метадату по файл айди
  /// </summary>
  /// <param name="fileId"></param>
  /// <returns></returns>
  [HttpGet("files/metadata/{fileId}")]
  public IActionResult GetFileMetadata(Guid fileId)
  {
    var request = new HttpRequestMessage(HttpMethod.Get, $"/api/files/metadata/{fileId}");
    var response = _fileStoringClient.Send(request);
    if (!response.IsSuccessStatusCode)
    {
      var error = response.Content.ReadAsStringAsync().Result;
      return StatusCode((int)response.StatusCode, new { error });
    }
    var json = response.Content.ReadAsStringAsync().Result;
    return Content(json, "application/json");
  }


  /// <summary>
  /// получаем облако слов (png) по номеру отчета
  /// </summary>
  /// <param name="reportId"></param>
  /// <returns></returns>
  [HttpGet("reports/{reportId}/wordcloud")]
  public IActionResult GetWordCloud(Guid reportId)
  {
    var request = new HttpRequestMessage(
        HttpMethod.Get,
        $"/api/analysis/{reportId}/wordcloud");

    var response = _fileAnalysisClient.Send(request);

    if (!response.IsSuccessStatusCode)
    {
      var errorContent = response.Content.ReadAsStringAsync().Result;
      return StatusCode((int)response.StatusCode, new { error = errorContent });
    }

    var fileBytes = response.Content.ReadAsByteArrayAsync().Result;
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/png";
    var fileName = $"{reportId}.png";

    return File(fileBytes, contentType, fileName);
  }
}