namespace SD3_FileAnalysisService.Models.Values;

public readonly record struct AssignmentId
{
  public string Value { get; init; }

  public AssignmentId(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new ArgumentException("Assignment ID must not be null or empty.", nameof(value));
    }

    if (value.Length > 100)
    {
      throw new ArgumentException("Assignment ID is too long.", nameof(value));
    }

    Value = value;
  }

  public override string ToString()
  {
    return Value;
  }

  public static implicit operator string(AssignmentId id) 
  {
    return id.ToString();
  }
  public static implicit operator AssignmentId(string value)
  {
    return new AssignmentId(value);
  }
}