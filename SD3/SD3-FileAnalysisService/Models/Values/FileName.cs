namespace SD3_FileAnalysisService.Models.Values;

public readonly record struct FileName
{
  public string Value { get; init; }

  public FileName(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new ArgumentException("File name must not be null or empty.", nameof(value));
    }

    if (value.Any(symb => Path.GetInvalidFileNameChars().Contains(symb)))
    {
      throw new ArgumentException("File name contains invalid symbols.", nameof(value));
    }


    Value = value;
  }

  public override string ToString()
  {
    return Value;
  }

  public static implicit operator string(FileName name)
  {
    return name.ToString();
  }
  public static implicit operator FileName(string value)
  {
    return new FileName(value);
  }
}