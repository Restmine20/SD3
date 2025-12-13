namespace SD3_FileAnalysisService.Models.Values;

public readonly record struct FilePath
{
  public string Value { get; init; }

  public FilePath(string value)
  {
    if (string.IsNullOrWhiteSpace(value)) {
      throw new ArgumentException("File path must not be null or empty.", nameof(value));
    }

    if (value.Any(symb => Path.GetInvalidPathChars().Contains(symb)))
    {
      throw new ArgumentException("File path contains invalid symbols.", nameof(value));
    }

    if (!Path.IsPathRooted(value))
    {
      throw new ArgumentException("File path must be absolute.", nameof(value));
    }

    Value = value;
  }

  public override string ToString()
  {
    return Value;
  }

  public static implicit operator string(FilePath path)
  {
    return path.ToString();
  }
  public static implicit operator FilePath(string value)
  {
    return new FilePath(value);
  }
}