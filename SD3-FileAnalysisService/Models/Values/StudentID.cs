namespace SD3_FileAnalysisService.Models.Values;

public readonly record struct StudentId
{
  public string Value { get; init; }

  public StudentId(string value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new ArgumentException("StudentID must not be null or empty.", nameof(value));
    }
    if (value.Length > 100)
    {
      throw new ArgumentException("Student ID is too long.", nameof(value));
    }

    Value = value;
  }

  public override string ToString()
  {
    return Value;
  }

  public static implicit operator string(StudentId id)
  {
    return id.ToString();
  }
  public static implicit operator StudentId(string value)
  {
    return new StudentId(value);
  }
}