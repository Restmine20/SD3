namespace SD3_FileAnalysisService.Models.Values;

public record WordCloudPath(string Value)
{
  public override string ToString() => Value;

  public static implicit operator string(WordCloudPath path) => path.Value;
  public static implicit operator WordCloudPath(string value) => new(value);
}