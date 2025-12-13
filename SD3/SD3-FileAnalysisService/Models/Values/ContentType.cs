namespace SD3_FileAnalysisService.Models.Values;

public readonly record struct ContentType
{
  public string Value { get; init; }

  public ContentType(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new ArgumentException("File path must not be null or empty.", nameof(value));
    }


    Value = value;
  }

  public override string ToString()
  {
    return Value;
  }

  public static implicit operator string(ContentType contentType)
  {
    return contentType.ToString();
  }
  public static implicit operator ContentType(string value)
  {
    return new ContentType(value);
  }
}